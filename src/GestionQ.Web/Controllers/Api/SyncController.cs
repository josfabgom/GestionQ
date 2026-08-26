using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using GestionQ.Domain.DTOs;
using GestionQ.Domain.Entities;
using GestionQ.Infrastructure.Data;
using System.Collections.Generic;

namespace GestionQ.Web.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class SyncController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SyncController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<PointOfSale> GetOrCreatePosAsync(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) identifier = "UNKNOWN-POS";
            var pos = await _context.PointsOfSale.FirstOrDefaultAsync(p => p.PosIdentifier == identifier);
            if (pos == null)
            {
                pos = new PointOfSale
                {
                    Name = "Caja " + identifier,
                    PosNumber = _context.PointsOfSale.Max(p => (int?)p.PosNumber) + 1 ?? 1,
                    PosIdentifier = identifier,
                    MachineName = identifier,
                    IsActive = true,
                    SyncOnlyWithStock = true,
                    SyncCustomers = true
                };
                _context.PointsOfSale.Add(pos);
                await _context.SaveChangesAsync();
            }
            return pos;
        }

        private async Task TrackSyncAsync(PointOfSale pos)
        {
            pos.LastSyncDate = DateTime.Now;
            pos.SyncIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _context.SaveChangesAsync();
        }

        [HttpPost("pull")]
        public async Task<ActionResult<SyncPullResponse>> Pull([FromBody] SyncPullRequest request)
        {
            var pos = await GetOrCreatePosAsync(request.PosIdentifier);
            await TrackSyncAsync(pos);

            var query = _context.Products.AsNoTracking().AsQueryable();
            
            if (request.LastSyncDate.HasValue)
            {
                query = query.Where(p => p.LastModified >= request.LastSyncDate.Value);
            }

            if (pos.SyncOnlyWithStock)
            {
                query = query.Where(p => p.Price > 0 && (p.Stock > 0 || p.IsDepartment));
            }

            var products = await query.Select(p => new ProductSyncDto
            {
                Id = p.Id,
                InternalCode = p.InternalCode,
                Barcode = p.Barcode,
                Name = p.Name,
                Price = p.Price,
                Stock = p.Stock,
                IsActive = p.IsActive,
                LastModified = p.LastModified
            }).ToListAsync();

            var customers = new List<CustomerSyncDto>();
            if (pos.SyncCustomers)
            {
                customers = await _context.Customers.AsNoTracking().Select(c => new CustomerSyncDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Email = c.Email,
                    Phone = c.Phone,
                    Dni = c.Dni,
                    Cuit = c.Cuit,
                    IsActive = c.IsActive
                }).ToListAsync();
            }

            var departments = await _context.Departments.AsNoTracking().Select(d => new DepartmentSyncDto
            {
                Id = d.Id,
                Name = d.Name,
                Hotkey = d.Hotkey,
                VirtualProductId = d.VirtualProductId
            }).ToListAsync();
            
            var paymentMethods = await _context.PaymentMethods.AsNoTracking().Select(pm => new PaymentMethodSyncDto
            {
                Id = pm.Id,
                Name = pm.Name,
                IsActive = pm.IsActive,
                DiscountPercentage = pm.DiscountPercentage
            }).ToListAsync();

            return Ok(new SyncPullResponse { 
                Products = products,
                Customers = customers,
                Departments = departments,
                PaymentMethods = paymentMethods
            });
        }

        [HttpPost("push")]
        public async Task<IActionResult> Push([FromBody] SyncPushRequest request)
        {
            var pos = await GetOrCreatePosAsync(request.PosIdentifier);
            await TrackSyncAsync(pos);

            // Process New Customers
            foreach (var custDto in request.NewCustomers)
            {
                if (string.IsNullOrEmpty(custDto.Dni)) continue;
                
                var exists = await _context.Customers.AnyAsync(c => c.Dni == custDto.Dni);
                if (!exists)
                {
                    _context.Customers.Add(new Customer
                    {
                        Name = custDto.Name,
                        Dni = custDto.Dni,
                        Email = custDto.Email,
                        Phone = custDto.Phone,
                        Cuit = custDto.Cuit,
                        IsActive = true
                    });
                }
            }
            await _context.SaveChangesAsync(); // Save so we can get IDs for sales

            // Process Sales
            foreach (var saleDto in request.Sales)
            {
                var exists = await _context.Sales.AnyAsync(s => s.GlobalId == saleDto.GlobalId);
                if (exists) continue;

                int? customerId = null;
                if (!string.IsNullOrEmpty(saleDto.CustomerDni))
                {
                    var cust = await _context.Customers.FirstOrDefaultAsync(c => c.Dni == saleDto.CustomerDni);
                    if (cust != null) customerId = cust.Id;
                }

                var sale = new Sale
                {
                    GlobalId = saleDto.GlobalId,
                    Date = saleDto.Date,
                    TotalAmount = saleDto.TotalAmount,
                    SubTotal = saleDto.SubTotal,
                    DiscountAmount = saleDto.DiscountAmount,
                    UserId = saleDto.UserId,
                    CashRegisterId = saleDto.CashRegisterId,
                    CustomerId = customerId,
                    PointOfSaleId = pos.Id,
                    IsSynced = true,
                    SyncedAt = DateTime.Now,
                    Items = saleDto.Items.Select(i => new SaleItem
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        DiscountAmount = i.DiscountAmount
                    }).ToList(),
                    Payments = saleDto.Payments.Select(p => new SalePayment
                    {
                        PaymentMethodId = p.PaymentMethodId,
                        Amount = p.Amount,
                        TransactionReference = p.TransactionReference
                    }).ToList()
                };

                _context.Sales.Add(sale);

                foreach (var item in saleDto.Items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null && !product.IsDepartment)
                    {
                        decimal previousStock = product.Stock;
                        product.Stock -= item.Quantity;
                        _context.Products.Update(product);

                        _context.StockMovements.Add(new StockMovement
                        {
                            Date = DateTime.Now,
                            ProductId = product.Id,
                            Quantity = -item.Quantity,
                            Type = MovementType.Sale,
                            Concept = $"Venta offline (Caja POS) de {item.Quantity} un.",
                            PreviousStock = previousStock,
                            NewStock = product.Stock
                        });
                    }
                }
            }

            foreach (var movDto in request.Movements)
            {
                var exists = await _context.CashRegisterMovements.AnyAsync(m => m.GlobalId == movDto.GlobalId);
                if (exists) continue;


                var movement = new CashRegisterMovement
                {
                    GlobalId = movDto.GlobalId,
                    Amount = movDto.Amount,
                    Type = movDto.Type,
                    Description = movDto.Description,
                    Date = movDto.Date,
                    CashRegisterId = movDto.CashRegisterId ?? 1,
                    IsSynced = true,
                    SyncedAt = DateTime.Now
                };
                
                _context.CashRegisterMovements.Add(movement);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
