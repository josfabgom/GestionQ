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
        private readonly Microsoft.Extensions.Configuration.IConfiguration _config;
        private readonly Microsoft.AspNetCore.Identity.UserManager<Microsoft.AspNetCore.Identity.IdentityUser> _userManager;

        public SyncController(ApplicationDbContext context, Microsoft.Extensions.Configuration.IConfiguration config, Microsoft.AspNetCore.Identity.UserManager<Microsoft.AspNetCore.Identity.IdentityUser> userManager)
        {
            _context = context;
            _config = config;
            _userManager = userManager;
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
                LastModified = p.LastModified,
                ImageUrl = p.ImageUrl
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

            var companyInfo = new CompanyInfoSyncDto
            {
                Name = _config["CompanyInfo:Name"] ?? "GestionQ",
                LogoUrl = "/images/logo.png"
            };

            var activePromos = await _context.PromotionRules
                .Include(p => p.Products)
                .ThenInclude(p => p.Product)
                .Where(p => p.IsActive)
                .Select(p => new PromotionSyncDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Details = p.Type == GestionQ.Domain.Entities.PromotionType.Percentage ? $"Descuento: {p.Value}%" :
                              p.Type == GestionQ.Domain.Entities.PromotionType.FixedAmount ? $"Descuento: ${p.Value}" :
                              p.Type == GestionQ.Domain.Entities.PromotionType.XForY ? $"Lleva {p.BuyQuantity} Paga {p.PayQuantity}" :
                              $"Volumen: {p.Value}",
                    // We append the products string to Details
                })
                .ToListAsync();
            
            // Re-fetch to format details locally (EF Core string.Join limitation workaround)
            var activePromosEntities = await _context.PromotionRules.Include(p => p.Products).ThenInclude(p => p.Product).Where(p => p.IsActive).ToListAsync();
            var activePromosResult = activePromosEntities.Select(p => new PromotionSyncDto {
                Id = p.Id,
                Name = "• " + p.Name,
                Details = (p.Type == GestionQ.Domain.Entities.PromotionType.Percentage ? $"Descuento: {p.Value}%" :
                           p.Type == GestionQ.Domain.Entities.PromotionType.FixedAmount ? $"Descuento: ${p.Value}" :
                           p.Type == GestionQ.Domain.Entities.PromotionType.XForY ? $"Lleva {p.BuyQuantity} Paga {p.PayQuantity}" :
                           $"Volumen: {p.Value}") + "\r\nProductos aplicables:\r\n" + string.Join("\r\n", p.Products.Select(pr => "- " + pr.Product?.Name)) + 
                           $"\r\nVálido hasta: {(p.EndDate.HasValue ? p.EndDate.Value.ToString("dd/MM/yyyy") : "Sin límite")}",
                Type = p.Type.ToString(),
                Value = p.Value,
                BuyQuantity = p.BuyQuantity,
                PayQuantity = p.PayQuantity,
                ProductIds = p.Products.Select(pr => pr.ProductId).ToList()
            }).ToList();

            var posUsers = new List<PosUserSyncDto>();
            var allUsers = await _userManager.Users.ToListAsync();
            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Cajero") || roles.Contains("Vendedor") || roles.Contains("Admin"))
                {
                    var claims = await _userManager.GetClaimsAsync(user);
                    var pinClaim = claims.FirstOrDefault(c => c.Type == "UserPin");
                    var fullNameClaim = claims.FirstOrDefault(c => c.Type == "FullName");
                    if (pinClaim != null)
                    {
                        posUsers.Add(new PosUserSyncDto
                        {
                            Id = user.Id,
                            UserName = user.UserName ?? string.Empty,
                            FullName = fullNameClaim?.Value ?? user.UserName ?? string.Empty,
                            Pin = pinClaim.Value
                        });
                    }
                }
            }

            return Ok(new SyncPullResponse { 
                Products = products,
                Customers = customers,
                Departments = departments,
                PaymentMethods = paymentMethods,
                CompanyInfo = companyInfo,
                ActivePromotions = activePromosResult,
                Users = posUsers
            });
        }

        [HttpPost("push")]
        public async Task<IActionResult> Push([FromBody] SyncPushRequest request)
        {
            var pos = await GetOrCreatePosAsync(request.PosIdentifier);
            await TrackSyncAsync(pos);

            var registerIdMap = new Dictionary<Guid, int>();

            foreach (var regDto in request.OfflineCashRegisters)
            {
                var existingRegister = await _context.CashRegisters
                    .FirstOrDefaultAsync(c => c.UserId == regDto.UserId 
                                         && c.PointOfSaleId == pos.Id 
                                         && Math.Abs(EF.Functions.DateDiffSecond(c.OpeningDate, regDto.OpeningDate)) < 5);

                if (existingRegister == null)
                {
                    existingRegister = new CashRegister
                    {
                        UserId = regDto.UserId,
                        PointOfSaleId = pos.Id,
                        OpeningDate = regDto.OpeningDate,
                        ClosingDate = regDto.ClosingDate,
                        InitialBalance = regDto.InitialBalance,
                        FinalCashBalance = regDto.FinalCashBalance
                    };
                    _context.CashRegisters.Add(existingRegister);
                    await _context.SaveChangesAsync();
                }
                
                registerIdMap[regDto.GlobalId] = existingRegister.Id;
            }

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
            _context.IgnoreStockChangesForLogging = true;
            foreach (var saleDto in request.Sales)
            {
                var existingSale = await _context.Sales.Include(s => s.Items).FirstOrDefaultAsync(s => s.GlobalId == saleDto.GlobalId);
                if (existingSale != null)
                {
                    if (saleDto.IsCancelled && !existingSale.IsCancelled)
                    {
                        existingSale.IsCancelled = true;
                        existingSale.CancellationDate = saleDto.CancellationDate ?? DateTime.Now;
                        _context.Sales.Update(existingSale);

                        // Reverse stock
                        foreach (var item in existingSale.Items)
                        {
                            var product = await _context.Products.FindAsync(item.ProductId);
                            if (product != null && !product.IsDepartment)
                            {
                                decimal previousStock = product.Stock;
                                product.Stock += item.Quantity;
                                _context.Products.Update(product);

                                _context.StockMovements.Add(new StockMovement
                                {
                                    Date = DateTime.Now,
                                    ProductId = product.Id,
                                    Quantity = item.Quantity,
                                    Type = MovementType.Return,
                                    Concept = $"Anulación de Venta (Sincronización POS) de {item.Quantity} un.",
                                    PreviousStock = previousStock,
                                    NewStock = product.Stock
                                });
                            }
                        }
                    }
                    continue;
                }

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
                    PaymentDiscountAmount = saleDto.PaymentDiscountAmount,
                    UserId = saleDto.UserId,
                    CashRegisterId = saleDto.CashRegisterId ?? (saleDto.OfflineCashRegisterGlobalId.HasValue && registerIdMap.ContainsKey(saleDto.OfflineCashRegisterGlobalId.Value) ? registerIdMap[saleDto.OfflineCashRegisterGlobalId.Value] : null),
                    CustomerId = customerId,
                    PointOfSaleId = pos.Id,
                    IsSynced = true,
                    SyncedAt = DateTime.Now,
                    RequestElectronicInvoice = saleDto.RequestElectronicInvoice,
                    IsCancelled = saleDto.IsCancelled,
                    CancellationDate = saleDto.CancellationDate,
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

                if (!saleDto.IsCancelled)
                {
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
                    CashRegisterId = movDto.CashRegisterId ?? (movDto.OfflineCashRegisterGlobalId.HasValue && registerIdMap.ContainsKey(movDto.OfflineCashRegisterGlobalId.Value) ? registerIdMap[movDto.OfflineCashRegisterGlobalId.Value] : 1),
                    IsSynced = true,
                    SyncedAt = DateTime.Now
                };
                
                _context.CashRegisterMovements.Add(movement);
            }

            await _context.SaveChangesAsync();

            foreach (var regDto in request.OfflineCashRegisters.Where(r => r.ClosingDate != null))
            {
                if (registerIdMap.TryGetValue(regDto.GlobalId, out int serverRegId))
                {
                    var register = await _context.CashRegisters
                        .Include(c => c.Movements)
                        .Include(c => c.Sales).ThenInclude(s => s.Payments).ThenInclude(p => p.PaymentMethod)
                        .FirstOrDefaultAsync(c => c.Id == serverRegId);

                    if (register != null)
                    {
                        register.ClosingDate = regDto.ClosingDate;
                        register.FinalCashBalance = regDto.FinalCashBalance;

                        decimal totalEfectivoVentas = register.Sales.Where(s => !s.IsCancelled)
                            .SelectMany(s => s.Payments)
                            .Where(p => p.PaymentMethod != null && p.PaymentMethod.Name == "Efectivo")
                            .Sum(p => p.Amount);

                        decimal totalIngresos = register.Movements
                            .Where(m => m.Type == "Ingreso")
                            .Sum(m => m.Amount);

                        decimal totalEgresos = register.Movements
                            .Where(m => m.Type == "Egreso")
                            .Sum(m => m.Amount);

                        register.ExpectedCashBalance = register.InitialBalance + totalEfectivoVentas + totalIngresos - totalEgresos;
                        register.Difference = regDto.FinalCashBalance - register.ExpectedCashBalance;

                        _context.CashRegisters.Update(register);
                    }
                }
            }
            await _context.SaveChangesAsync();

            return Ok(new SyncPushResponse { Success = true, RegisterIdMap = registerIdMap });
        }
    }
}

