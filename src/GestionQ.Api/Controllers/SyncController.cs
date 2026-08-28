using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using GestionQ.Domain.DTOs;
using GestionQ.Domain.Entities;
using GestionQ.Infrastructure.Data;
using GestionQ.Infrastructure.Services;

namespace GestionQ.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SyncController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IElectronicInvoicingService _invoicingService;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _config;

        public SyncController(ApplicationDbContext context, IElectronicInvoicingService invoicingService, Microsoft.Extensions.Configuration.IConfiguration config)
        {
            _context = context;
            _invoicingService = invoicingService;
            _config = config;
        }

        [HttpPost("pull")]
        public async Task<ActionResult<SyncPullResponse>> Pull([FromBody] SyncPullRequest request)
        {
            var query = _context.Products.AsNoTracking().AsQueryable();
            
            var approvedSetting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == "LastApprovedProductSyncDate");
            DateTime maxDate = approvedSetting != null && DateTime.TryParse(approvedSetting.Value, out var parsed) 
                ? parsed 
                : DateTime.MinValue; // If not set, don't send any products (must be manually authorized first)

            query = query.Where(p => p.LastModified <= maxDate);

            if (request.LastSyncDate.HasValue)
            {
                query = query.Where(p => p.LastModified > request.LastSyncDate.Value);
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

            // Sincronizar clientes
            var customers = await _context.Customers.AsNoTracking().Select(c => new CustomerSyncDto
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email,
                Phone = c.Phone,
                Dni = c.Dni,
                Cuit = c.Cuit,
                IsActive = c.IsActive
            }).ToListAsync();

            // Sincronizar departamentos
            var departments = await _context.Departments.AsNoTracking().Select(d => new DepartmentSyncDto
            {
                Id = d.Id,
                Name = d.Name,
                Hotkey = d.Hotkey,
                VirtualProductId = d.VirtualProductId
            }).ToListAsync();

            return Ok(new SyncPullResponse { 
                Products = products,
                Customers = customers,
                Departments = departments
            });
        }

        [HttpPost("push")]
        public async Task<IActionResult> Push([FromBody] SyncPushRequest request)
        {
            // Process Sales
            foreach (var saleDto in request.Sales)
            {
                // Check if already exists
                var exists = await _context.Sales.AnyAsync(s => s.GlobalId == saleDto.GlobalId);
                if (exists) continue;

                var sale = new Sale
                {
                    GlobalId = saleDto.GlobalId,
                    Date = saleDto.Date,
                    TotalAmount = saleDto.TotalAmount,
                    SubTotal = saleDto.SubTotal,
                    DiscountAmount = saleDto.DiscountAmount,
                    UserId = saleDto.UserId,
                    IsSynced = true,
                    SyncedAt = DateTime.Now,
                    RequestElectronicInvoice = saleDto.RequestElectronicInvoice,
                    Items = saleDto.Items.Select(i => new SaleItem
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        DiscountAmount = i.DiscountAmount
                    }).ToList()
                };

                _context.Sales.Add(sale);
            }

            // Process Movements
            foreach (var movDto in request.Movements)
            {
                var exists = await _context.CashRegisterMovements.AnyAsync(m => m.GlobalId == movDto.GlobalId);
                if (exists) continue;

                // Needs a default CashRegisterId for offline sync, or it should come from DTO.
                // For simplicity, we use the first available register or assign 1.
                var register = await _context.CashRegisters.FirstOrDefaultAsync();
                
                var movement = new CashRegisterMovement
                {
                    GlobalId = movDto.GlobalId,
                    Amount = movDto.Amount,
                    Type = movDto.Type,
                    Description = movDto.Description,
                    Date = movDto.Date,
                    CashRegisterId = register?.Id ?? 1,
                    IsSynced = true,
                    SyncedAt = DateTime.Now
                };
                
                _context.CashRegisterMovements.Add(movement);
            }

            await _context.SaveChangesAsync();

            // Try generating electronic invoices for those requested
            foreach (var saleDto in request.Sales.Where(s => s.RequestElectronicInvoice))
            {
                var dbSale = await _context.Sales
                    .Include(s => s.Customer).ThenInclude(c => c.TaxCondition)
                    .Include(s => s.PointOfSale)
                    .Include(s => s.Items).ThenInclude(si => si.Product).ThenInclude(p => p.VatRate)
                    .FirstOrDefaultAsync(s => s.GlobalId == saleDto.GlobalId);
                
                if (dbSale != null)
                {
                    try {
                        await GenerateElectronicInvoiceForSale(dbSale);
                        await _context.SaveChangesAsync();
                    } catch (Exception ex) {
                        Console.WriteLine($"Error AFIP en Sync: {ex.Message}");
                    }
                }
            }

            return Ok();
        }
        private async Task GenerateElectronicInvoiceForSale(Sale sale)
        {
            if (sale.ElectronicInvoice != null && sale.ElectronicInvoice.Status == "Approved") return;

            decimal netAmount = 0, vatAmount = 0, exemptAmount = 0;
            foreach (var item in sale.Items)
            {
                decimal itemTotal = item.Quantity * item.UnitPrice;
                decimal vatRatePercent = item.Product?.VatRate?.Rate ?? 21.0m;
                if (vatRatePercent == 0) exemptAmount += itemTotal;
                else { decimal net = itemTotal / (1 + (vatRatePercent / 100)); vatAmount += (itemTotal - net); netAmount += net; }
            }

            int defaultInvoiceTypeCode = 6, defaultCondicionIvaReceptor = 5, docTypeCode = 99;
            string customerCuit = sale.Customer?.Cuit ?? "", customerDni = sale.Customer?.Dni ?? "";
            if (!string.IsNullOrWhiteSpace(customerCuit)) docTypeCode = 80; else if (!string.IsNullOrWhiteSpace(customerDni)) docTypeCode = 96;

            var companyTaxCondition = _config["CompanyInfo:TaxCondition"]?.ToLower() ?? "";
            bool isMonotributista = companyTaxCondition.Contains("monotributo") || companyTaxCondition.Contains("monotributista");

            if (isMonotributista) {
                defaultInvoiceTypeCode = 11;
                if (sale.Customer?.TaxCondition != null) {
                    var tName = sale.Customer.TaxCondition.Name.ToLower();
                    if (tName.Contains("inscripto")) defaultCondicionIvaReceptor = 1;
                    else if (tName.Contains("monotributo")) defaultCondicionIvaReceptor = 6;
                    else if (tName.Contains("exento")) defaultCondicionIvaReceptor = 4;
                }
            } else {
                if (sale.Customer?.TaxCondition != null) {
                    var tName = sale.Customer.TaxCondition.Name.ToLower();
                    if (tName.Contains("inscripto")) { defaultInvoiceTypeCode = 1; defaultCondicionIvaReceptor = 1; }
                    else if (tName.Contains("monotributo")) { defaultInvoiceTypeCode = 6; defaultCondicionIvaReceptor = 6; }
                    else if (tName.Contains("exento")) { defaultInvoiceTypeCode = 6; defaultCondicionIvaReceptor = 4; }
                }
            }

            var posId = sale.PointOfSaleId ?? 0;
            var posNumber = sale.PointOfSale?.PosNumber ?? 1;
            if (posId == 0) { var defaultPos = await _context.PointsOfSale.FirstOrDefaultAsync(); if (defaultPos != null) { posId = defaultPos.Id; posNumber = defaultPos.PosNumber; } }

            var request = new GestionQ.Infrastructure.Services.ElectronicInvoiceRequest {
                PointOfSaleId = posId, PointOfSaleNumber = posNumber, InvoiceTypeCode = defaultInvoiceTypeCode, ConceptCode = 1, DocTypeCode = docTypeCode,
                DocNumber = !string.IsNullOrWhiteSpace(customerCuit) ? customerCuit : (!string.IsNullOrWhiteSpace(customerDni) ? customerDni : "0"),
                CustomerName = sale.Customer?.Name ?? "Consumidor Final", CustomerTaxCondition = sale.Customer?.TaxCondition?.Name ?? "Consumidor Final",
                NetAmount = netAmount, VatAmount = vatAmount, ExemptAmount = exemptAmount, TotalAmount = sale.TotalAmount, CondicionIVAReceptorId = defaultCondicionIvaReceptor
            };

            var response = await _invoicingService.RequestCAEAsync(request);
            var ei = new ElectronicInvoice {
                SaleId = sale.Id, PointOfSaleId = posId, PointOfSaleNumber = posNumber, InvoiceTypeCode = defaultInvoiceTypeCode, InvoiceNumber = response.InvoiceNumber,
                IssueDate = DateTime.Now, TotalAmount = sale.TotalAmount, NetAmount = netAmount, VatAmount = vatAmount, ExemptAmount = exemptAmount,
                Status = response.Status, CAE = response.CAE, CAEExpirationDate = response.CAEExpirationDate != default ? response.CAEExpirationDate : DateTime.Now,
                ErrorMessage = string.Join(" | ", response.Errors)
            };
            _context.ElectronicInvoices.Add(ei);
        }
    }
}

