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
        private readonly GestionQ.Web.Services.IArcaAutomationService _arcaAutomation;

        public SyncController(
            ApplicationDbContext context,
            Microsoft.Extensions.Configuration.IConfiguration config,
            Microsoft.AspNetCore.Identity.UserManager<Microsoft.AspNetCore.Identity.IdentityUser> userManager,
            GestionQ.Web.Services.IArcaAutomationService arcaAutomation)
        {
            _context = context;
            _config = config;
            _userManager = userManager;
            _arcaAutomation = arcaAutomation;
        }

        private async Task<PointOfSale> GetOrCreatePosAsync(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) identifier = "UNKNOWN-POS";
            var pos = await _context.PointsOfSale.FirstOrDefaultAsync(p => p.PosIdentifier == identifier);
            if (pos != null) return pos;

            // Si existe un Punto de Venta activo pre-creado en Gestión Web sin PC vinculada, vincularlo automáticamente
            var unassignedPos = await _context.PointsOfSale
                .Where(p => p.IsActive && string.IsNullOrEmpty(p.PosIdentifier))
                .OrderBy(p => p.PosNumber)
                .FirstOrDefaultAsync();

            if (unassignedPos != null)
            {
                unassignedPos.PosIdentifier = identifier;
                if (string.IsNullOrEmpty(unassignedPos.MachineName))
                {
                    unassignedPos.MachineName = identifier;
                }
                await _context.SaveChangesAsync();
                return unassignedPos;
            }

            // Buscar el menor número de Punto de Venta libre (1, 2, 3...) para evitar saltos por POS 99/pruebas
            var usedNumbers = new HashSet<int>(await _context.PointsOfSale.AsNoTracking().Select(p => p.PosNumber).ToListAsync());
            int nextPosNumber = 1;
            while (usedNumbers.Contains(nextPosNumber))
            {
                nextPosNumber++;
            }

            pos = new PointOfSale
            {
                Name = "Caja " + identifier,
                PosNumber = nextPosNumber,
                PosIdentifier = identifier,
                MachineName = identifier,
                IsActive = true,
                SyncOnlyWithStock = true,
                SyncCustomers = true
            };
            _context.PointsOfSale.Add(pos);
            await _context.SaveChangesAsync();
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
                query = query.Where(p => (p.Price > 0 && p.Stock > 0) || p.IsDepartment);
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

            var defaultPmSetting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == "DefaultPaymentMethodId");
            int? defaultPmId = null;
            if (defaultPmSetting != null && int.TryParse(defaultPmSetting.Value, out int parsedId))
            {
                defaultPmId = parsedId;
            }

            var dniThresholdSetting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == "Afip_ConsumerFinalDniThreshold");
            decimal dniThreshold = 0m;
            if (dniThresholdSetting != null)
            {
                decimal.TryParse(dniThresholdSetting.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out dniThreshold);
            }

            var companyInfo = new CompanyInfoSyncDto
            {
                Name = _config["CompanyInfo:Name"] ?? "GestionQ",
                LogoUrl = "/images/logo.png",
                DefaultPaymentMethodId = defaultPmId,
                ConsumerFinalDniThreshold = dniThreshold
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
                Name = "â€¢ " + p.Name,
                Details = (p.Type == GestionQ.Domain.Entities.PromotionType.Percentage ? $"Descuento: {p.Value}%" :
                           p.Type == GestionQ.Domain.Entities.PromotionType.FixedAmount ? $"Descuento: ${p.Value}" :
                           p.Type == GestionQ.Domain.Entities.PromotionType.XForY ? $"Lleva {p.BuyQuantity} Paga {p.PayQuantity}" :
                           $"Volumen: {p.Value}") + "\r\nProductos aplicables:\r\n" + string.Join("\r\n", p.Products.Select(pr => "- " + pr.Product?.Name)) + 
                           $"\r\nVÃ¡lido hasta: {(p.EndDate.HasValue ? p.EndDate.Value.ToString("dd/MM/yyyy") : "Sin lÃ­mite")}",
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

            var presentations = await _context.ProductPresentations.AsNoTracking()
                .Where(p => p.IsActive)
                .Select(p => new ProductPresentationSyncDto
                {
                    Id = p.Id,
                    ProductId = p.ProductId,
                    Name = p.Name,
                    Barcode = p.Barcode,
                    Quantity = p.Quantity,
                    Price = p.Price,
                    ProfitMargin = p.ProfitMargin,
                    IsBulk = p.IsBulk,
                    IsActive = p.IsActive
                }).ToListAsync();

            return Ok(new SyncPullResponse { 
                Products = products,
                ProductPresentations = presentations,
                Customers = customers,
                Departments = departments,
                PaymentMethods = paymentMethods,
                CompanyInfo = companyInfo,
                ActivePromotions = activePromosResult,
                Users = posUsers,
                PosNumber = pos.PosNumber,
                PosName = pos.Name
            });
        }

        private async Task<int> GetOrCreateDeletedFallbackProductIdAsync()
        {
            var fallback = await _context.Products.FirstOrDefaultAsync(p => p.Barcode == "DELETED-POS-SYNC");
            if (fallback == null)
            {
                fallback = new Product
                {
                    InternalCode = 0,
                    Barcode = "DELETED-POS-SYNC",
                    Name = "Producto Eliminado (Sync POS)",
                    Price = 0,
                    Stock = 0,
                    IsDepartment = false,
                    IsActive = false,
                    CreationDate = DateTime.Now,
                    LastModified = DateTime.Now
                };
                _context.Products.Add(fallback);
                await _context.SaveChangesAsync();
            }
            return fallback.Id;
        }

        [HttpPost("push")]
        public async Task<IActionResult> Push([FromBody] SyncPushRequest request)
        {
            try
            {
            var pos = await GetOrCreatePosAsync(request.PosIdentifier);
            await TrackSyncAsync(pos);

            var validUserIds = new HashSet<string>(await _userManager.Users.AsNoTracking().Select(u => u.Id).ToListAsync());
            string fallbackUserId = (await _userManager.FindByEmailAsync("admin@gestionq.com"))?.Id
                ?? validUserIds.FirstOrDefault()
                ?? string.Empty;

            foreach (var regDto in request.OfflineCashRegisters)
            {
                string safeRegUserId = (!string.IsNullOrEmpty(regDto.UserId) && validUserIds.Contains(regDto.UserId))
                    ? regDto.UserId
                    : fallbackUserId;

                var existingRegister = await _context.CashRegisters.FirstOrDefaultAsync(c => c.GlobalId == regDto.GlobalId);

                if (existingRegister == null)
                {
                    // If opening a new register, ensure no zombies exist for this POS!
                    if (regDto.ClosingDate == null)
                    {
                        var zombies = await _context.CashRegisters
                            .Where(c => c.PointOfSaleId == pos.Id && c.ClosingDate == null)
                            .ToListAsync();
                        
                        foreach (var zombie in zombies)
                        {
                            zombie.ClosingDate = DateTime.Now;
                            zombie.ExpectedCashBalance = zombie.InitialBalance; // Simplification for force close
                        }
                    }

                    existingRegister = new CashRegister
                    {
                        GlobalId = regDto.GlobalId,
                        UserId = safeRegUserId,
                        PointOfSaleId = pos.Id,
                        OpeningDate = regDto.OpeningDate,
                        ClosingDate = regDto.ClosingDate,
                        InitialBalance = regDto.InitialBalance,
                        FinalCashBalance = regDto.FinalCashBalance
                    };
                    _context.CashRegisters.Add(existingRegister);
                }
                else
                {
                    existingRegister.ClosingDate = regDto.ClosingDate;
                    if (regDto.FinalCashBalance > 0 || regDto.ClosingDate != null)
                        existingRegister.FinalCashBalance = regDto.FinalCashBalance;
                }
                
                await _context.SaveChangesAsync();
            }

            // Process New Customers
            foreach (var custDto in request.NewCustomers)
            {
                if (string.IsNullOrWhiteSpace(custDto.Dni) && string.IsNullOrWhiteSpace(custDto.Cuit)) continue;
                
                var exists = await _context.Customers.AnyAsync(c =>
                    (!string.IsNullOrWhiteSpace(custDto.Dni) && c.Dni == custDto.Dni)
                    || (!string.IsNullOrWhiteSpace(custDto.Cuit) && c.Cuit == custDto.Cuit));
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

            // Pre-cargar IDs válidos para evitar errores de Foreign Key (HTTP 500) por registros eliminados
            var validProductIds = new HashSet<int>(await _context.Products.AsNoTracking().Select(p => p.Id).ToListAsync());
            var validPresentationIds = new HashSet<int>(await _context.ProductPresentations.AsNoTracking().Select(p => p.Id).ToListAsync());
            var validRegisterIds = new HashSet<int>(await _context.CashRegisters.AsNoTracking().Select(c => c.Id).ToListAsync());
            var paymentMethodsList = await _context.PaymentMethods.AsNoTracking().Select(pm => new { pm.Id, pm.Name }).ToListAsync();
            var validPaymentMethodIds = new HashSet<int>(paymentMethodsList.Select(pm => pm.Id));
            int fallbackPaymentMethodId = paymentMethodsList.FirstOrDefault(pm => pm.Name == "Efectivo")?.Id
                ?? paymentMethodsList.FirstOrDefault()?.Id
                ?? 1;
            int? deletedFallbackProductId = null;

            // Process Sales
            var cancelledSaleIdsForNc = new List<int>();
            _context.IgnoreStockChangesForLogging = true;
            foreach (var saleDto in request.Sales)
            {
                var existingSale = await _context.Sales
                    .Include(s => s.Items)
                    .Include(s => s.ElectronicInvoice)
                    .FirstOrDefaultAsync(s => s.GlobalId == saleDto.GlobalId);
                if (existingSale != null)
                {
                    if (saleDto.IsCancelled && !existingSale.IsCancelled)
                    {
                        existingSale.IsCancelled = true;
                        existingSale.CancellationDate = saleDto.CancellationDate ?? DateTime.Now;
                        _context.Sales.Update(existingSale);
                        if (existingSale.ElectronicInvoice != null)
                        {
                            cancelledSaleIdsForNc.Add(existingSale.Id);
                        }

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
                                    Concept = $"AnulaciÃ³n de Venta (SincronizaciÃ³n POS) de {item.Quantity} un.",
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
                    var cust = await _context.Customers.FirstOrDefaultAsync(c => c.Dni == saleDto.CustomerDni || c.Cuit == saleDto.CustomerDni);
                    if (cust != null) customerId = cust.Id;
                }

                var saleItems = new List<SaleItem>();
                foreach (var i in saleDto.Items)
                {
                    int resolvedProductId = i.ProductId;
                    string? customName = null;
                    if (!validProductIds.Contains(resolvedProductId))
                    {
                        deletedFallbackProductId ??= await GetOrCreateDeletedFallbackProductIdAsync();
                        validProductIds.Add(deletedFallbackProductId.Value);
                        resolvedProductId = deletedFallbackProductId.Value;
                        customName = $"Producto eliminado (ID #{i.ProductId})";
                    }

                    saleItems.Add(new SaleItem
                    {
                        ProductId = resolvedProductId,
                        PresentationId = (i.PresentationId.HasValue && validPresentationIds.Contains(i.PresentationId.Value)) ? i.PresentationId : null,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        DiscountAmount = i.DiscountAmount,
                        CustomName = customName
                    });
                }

                string? safeSaleUserId = (!string.IsNullOrEmpty(saleDto.UserId) && validUserIds.Contains(saleDto.UserId))
                    ? saleDto.UserId
                    : (string.IsNullOrEmpty(fallbackUserId) ? null : fallbackUserId);

                var sale = new Sale
                {
                    GlobalId = saleDto.GlobalId,
                    PosTicketNumber = saleDto.PosTicketNumber,
                    Date = saleDto.Date,
                    TotalAmount = saleDto.TotalAmount,
                    SubTotal = saleDto.SubTotal,
                    DiscountAmount = saleDto.DiscountAmount,
                    PaymentDiscountAmount = saleDto.PaymentDiscountAmount,
                    UserId = safeSaleUserId,
                    CashRegisterId = null, // We will assign it below based on GlobalId
                    CustomerId = customerId,
                    PointOfSaleId = pos.Id,
                    IsSynced = true,
                    SyncedAt = DateTime.Now,
                    RequestElectronicInvoice = saleDto.RequestElectronicInvoice,
                    IsCancelled = saleDto.IsCancelled,
                    CancellationDate = saleDto.CancellationDate,
                    Items = saleItems,
                    Payments = saleDto.Payments.Select(p => new SalePayment
                    {
                        PaymentMethodId = validPaymentMethodIds.Contains(p.PaymentMethodId) ? p.PaymentMethodId : fallbackPaymentMethodId,
                        Amount = p.Amount,
                        TransactionReference = p.TransactionReference
                    }).ToList()
                };

                if (saleDto.OfflineCashRegisterGlobalId.HasValue)
                {
                    var targetRegister = await _context.CashRegisters.FirstOrDefaultAsync(c => c.GlobalId == saleDto.OfflineCashRegisterGlobalId.Value);
                    if (targetRegister != null)
                    {
                        sale.CashRegisterId = targetRegister.Id;
                    }
                }
                else if (saleDto.CashRegisterId.HasValue && validRegisterIds.Contains(saleDto.CashRegisterId.Value))
                {
                    sale.CashRegisterId = saleDto.CashRegisterId.Value;
                }

                _context.Sales.Add(sale);

                if (!saleDto.IsCancelled)
                {
                    foreach (var item in saleDto.Items)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null && !product.IsDepartment && product.Barcode != "DELETED-POS-SYNC")
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

                int? resolvedRegisterId = null;
                if (movDto.OfflineCashRegisterGlobalId.HasValue)
                {
                    var targetRegister = await _context.CashRegisters.FirstOrDefaultAsync(c => c.GlobalId == movDto.OfflineCashRegisterGlobalId.Value);
                    if (targetRegister != null)
                    {
                        resolvedRegisterId = targetRegister.Id;
                    }
                }
                else if (movDto.CashRegisterId.HasValue && validRegisterIds.Contains(movDto.CashRegisterId.Value))
                {
                    resolvedRegisterId = movDto.CashRegisterId.Value;
                }

                if (!resolvedRegisterId.HasValue)
                {
                    var fallbackRegister = await _context.CashRegisters
                        .Where(c => c.PointOfSaleId == pos.Id)
                        .OrderByDescending(c => c.OpeningDate)
                        .FirstOrDefaultAsync()
                        ?? await _context.CashRegisters.OrderByDescending(c => c.OpeningDate).FirstOrDefaultAsync();

                    resolvedRegisterId = fallbackRegister?.Id;
                }

                if (!resolvedRegisterId.HasValue) continue;

                var movement = new CashRegisterMovement
                {
                    GlobalId = movDto.GlobalId,
                    Amount = movDto.Amount,
                    Type = movDto.Type,
                    Description = movDto.Description,
                    Date = movDto.Date,
                    CashRegisterId = resolvedRegisterId.Value,
                    IsSynced = true,
                    SyncedAt = DateTime.Now
                };
                
                _context.CashRegisterMovements.Add(movement);
            }

            await _context.SaveChangesAsync();

            foreach (var regDto in request.OfflineCashRegisters.Where(r => r.ClosingDate != null))
            {
                var register = await _context.CashRegisters
                    .Include(c => c.Movements)
                    .Include(c => c.Sales).ThenInclude(s => s.Payments).ThenInclude(p => p.PaymentMethod)
                    .FirstOrDefaultAsync(c => c.GlobalId == regDto.GlobalId);

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
                    
                    // --- INTEGRACIÃ“N CAJA CENTRAL ---
                    var alreadySynced = await _context.CentralCashMovements.AnyAsync(m => m.SourceCashRegisterId == register.Id);
                    if (!alreadySynced)
                    {
                        decimal netoEfectivo = (register.FinalCashBalance ?? 0m) - register.InitialBalance;
                        if (netoEfectivo > 0)
                        {
                            _context.CentralCashMovements.Add(new CentralCashMovement
                            {
                                Date = DateTime.Now,
                                Type = "Ingreso",
                                Amount = netoEfectivo,
                                Concept = $"RendiciÃ³n Caja POS #{register.Id} - Efectivo",
                                UserId = register.UserId,
                                SourceCashRegisterId = register.Id
                            });
                        }
                        else if (netoEfectivo < 0)
                        {
                            _context.CentralCashMovements.Add(new CentralCashMovement
                            {
                                Date = DateTime.Now,
                                Type = "Egreso",
                                Amount = Math.Abs(netoEfectivo),
                                Concept = $"Faltante/Retiro Caja POS #{register.Id} - Efectivo",
                                UserId = register.UserId,
                                SourceCashRegisterId = register.Id
                            });
                        }

                        var otrosPagos = register.Sales.Where(s => !s.IsCancelled)
                            .SelectMany(s => s.Payments)
                            .Where(p => p.PaymentMethod != null && p.PaymentMethod.Name != "Efectivo")
                            .GroupBy(p => p.PaymentMethod!.Name)
                            .Select(g => new { Metodo = g.Key, Total = g.Sum(x => x.Amount) })
                            .Where(g => g.Total > 0)
                            .ToList();

                        foreach (var pago in otrosPagos)
                        {
                            _context.CentralCashMovements.Add(new CentralCashMovement
                            {
                                Date = DateTime.Now,
                                Type = "Ingreso",
                                Amount = pago.Total,
                                Concept = $"RendiciÃ³n Caja POS #{register.Id} - {pago.Metodo}",
                                UserId = register.UserId,
                                SourceCashRegisterId = register.Id
                            });
                        }
                    }
                }
            }
            await _context.SaveChangesAsync();

            // Emitir Nota de Crédito automática en ARCA para ventas facturadas que fueron anuladas desde la POS
            foreach (var cancelledSaleId in cancelledSaleIdsForNc)
            {
                try
                {
                    await _arcaAutomation.EmitCreditNoteForCancelledSaleAsync(cancelledSaleId);
                }
                catch
                {
                    // Si falla ARCA ahora, ArcaRetryBackgroundService lo reintentará automáticamente
                }
            }

            return Ok(new SyncPushResponse { Success = true });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.InnerException?.Message ?? ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { error = detail });
            }
        }
    }
}



