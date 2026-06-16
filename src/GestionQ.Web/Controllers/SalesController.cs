using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionQ.Infrastructure.Data;
using GestionQ.Domain.Entities;
using GestionQ.Web.Models;
using GestionQ.Infrastructure.Services;

using Microsoft.AspNetCore.Identity;

namespace GestionQ.Web.Controllers
{
    [Authorize]
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _config;
        private readonly IMercadoPagoService _mpService;
        private readonly IElectronicInvoicingService _electronicInvoicingService;

        public SalesController(
            ApplicationDbContext context, 
            UserManager<IdentityUser> userManager, 
            IConfiguration config,
            IMercadoPagoService mpService,
            IElectronicInvoicingService electronicInvoicingService)
        {
            _context = context;
            _userManager = userManager;
            _config = config;
            _mpService = mpService;
            _electronicInvoicingService = electronicInvoicingService;
        }

        public async Task<IActionResult> Index()
        {
            var sales = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.PointOfSale)
                .OrderByDescending(s => s.Date)
                .ToListAsync();
            return View(sales);
        }

        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var terminalPosId = Request.Cookies["TerminalPOSId"];
            if (string.IsNullOrEmpty(terminalPosId))
            {
                TempData["Message"] = "Esta PC no ha sido configurada como Punto de Venta. Por favor, identifícala primero.";
                return RedirectToAction("Index", "POSConfig");
            }

            var openRegister = await _context.CashRegisters
                .FirstOrDefaultAsync(c => c.UserId == user.Id && c.ClosingDate == null);

            if (openRegister == null)
            {
                TempData["Message"] = "No tienes una caja abierta. Por favor, realiza la apertura de tu turno para poder vender.";
                return RedirectToAction("Open", "CashRegisters");
            }

            ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).ToListAsync();
            ViewBag.Products = await _context.Products.Where(p => p.Stock > 0 && p.IsActive).ToListAsync();
            ViewBag.PaymentMethods = await _context.PaymentMethods.Where(p => p.IsActive).ToListAsync();
            ViewBag.Promotions = await _context.PromotionRules
                .Where(r => r.IsActive && r.StartDate <= DateTime.Now && r.EndDate >= DateTime.Now)
                .Include(r => r.Products)
                .ToListAsync();
            return View();
        }

        public async Task<IActionResult> Details(int id)
        {
            var sale = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.PointOfSale)
                .Include(s => s.Items)
                .ThenInclude(i => i.Product)
                .Include(s => s.Payments)
                .ThenInclude(p => p.PaymentMethod)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null) return NotFound();
            return View(sale);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSale([FromBody] SaleRequest model)
        {
            if (model == null || model.Items == null || !model.Items.Any())
                return BadRequest("No items in sale or invalid request.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return BadRequest("Usuario no válido.");

                var openRegister = await _context.CashRegisters
                    .FirstOrDefaultAsync(c => c.UserId == user.Id && c.ClosingDate == null);

                if (openRegister == null) return BadRequest("No existe una caja abierta para operar.");

                // Validar que la terminal actual coincida con la caja abierta
                var terminalPosId = Request.Cookies["TerminalPOSId"];
                if (string.IsNullOrEmpty(terminalPosId) || int.Parse(terminalPosId) != openRegister.PointOfSaleId)
                {
                    var currentPos = await _context.PointsOfSale.FindAsync(string.IsNullOrEmpty(terminalPosId) ? 0 : int.Parse(terminalPosId));
                    var openedPos = await _context.PointsOfSale.FindAsync(openRegister.PointOfSaleId);
                    
                    return BadRequest($"Conflicto de Terminal: Estás intentando vender desde '{currentPos?.Name ?? "Desconocida"}', pero tu caja abierta pertenece a '{openedPos?.Name ?? "Otra Terminal"}'. Por favor, vuelve a la PC original o cierra tu caja actual para abrir una nueva aquí.");
                }

                decimal subtotalBruto = 0;
                decimal totalPromoDiscounts = 0;
                var saleItems = new List<SaleItem>();

                foreach (var item in model.Items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null) return BadRequest("Producto no encontrado");
                    if (product.Stock < item.Quantity)
                        return BadRequest($"Stock insuficiente para {product.Name}");

                    var itemBruto = product.Price * item.Quantity;
                    subtotalBruto += itemBruto;
                    totalPromoDiscounts += item.DiscountAmount;

                    saleItems.Add(new SaleItem
                    {
                        ProductId = product.Id,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price,
                        DiscountAmount = item.DiscountAmount
                    });

                    decimal previousStock = product.Stock;
                    product.Stock -= item.Quantity;
                    _context.Products.Update(product);

                    // Record Stock Movement
                    _context.StockMovements.Add(new StockMovement
                    {
                        Date = DateTime.Now,
                        ProductId = product.Id,
                        Quantity = -item.Quantity,
                        Type = MovementType.Sale,
                        Concept = $"Venta de {item.Quantity} un.",
                        PreviousStock = previousStock,
                        NewStock = product.Stock
                    });
                }

                // Calcular descuento general (cliente o manual)
                decimal generalDiscount = 0;
                if (model.DiscountPercentage > 0)
                {
                    generalDiscount = (subtotalBruto - totalPromoDiscounts) * (model.DiscountPercentage / 100);
                }
                else if (model.DiscountAmount > totalPromoDiscounts)
                {
                    generalDiscount = model.DiscountAmount - totalPromoDiscounts;
                }

                decimal totalDiscount = totalPromoDiscounts + generalDiscount;
                decimal netTotal = Math.Max(0, subtotalBruto - totalDiscount);
                decimal paymentDiscount = model.PaymentDiscountAmount;
                decimal finalTotal = Math.Max(0, netTotal - paymentDiscount);

                var sale = new Sale
                {
                    Date = DateTime.Now,
                    CustomerId = model.CustomerId > 0 ? model.CustomerId : null,
                    SubTotal = subtotalBruto,
                    DiscountAmount = totalDiscount,
                    PaymentDiscountAmount = paymentDiscount,
                    TotalAmount = finalTotal,
                    UserId = user.Id,
                    CashRegisterId = openRegister.Id,
                    PointOfSaleId = openRegister.PointOfSaleId,
                    Items = saleItems,
                    Payments = model.Payments?.Select(p => new SalePayment
                    {
                        PaymentMethodId = p.PaymentMethodId,
                        Amount = p.Amount,
                        TransactionReference = p.TransactionReference
                    }).ToList() ?? new List<SalePayment>()
                };

                if (sale.CustomerId.HasValue)
                {
                    var customer = await _context.Customers.FindAsync(sale.CustomerId.Value);
                    if (customer != null)
                    {
                        if (!customer.IsActive)
                            return BadRequest("El cliente seleccionado no está activo para operar.");

                        // Decrease balance indicating they owe us more (cuenta corriente)
                        decimal totalPaid = sale.Payments.Sum(p => p.Amount);
                        decimal difference = sale.TotalAmount - totalPaid;
                        customer.Balance -= difference;
                        _context.Customers.Update(customer);
                    }
                }

                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Ok(sale.Id);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest($"Error: {ex.Message}");
            }
        }

        public async Task<IActionResult> PrintTicket(int id)
        {
            var sale = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.PointOfSale)
                .Include(s => s.Items)
                .ThenInclude(i => i.Product)
                .Include(s => s.Payments)
                .ThenInclude(p => p.PaymentMethod)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null) return NotFound();

            // Buscar nombres de promociones aplicadas a la venta
            var productIds = sale.Items.Select(i => i.ProductId).ToList();
            var promotions = await _context.PromotionRules
                .Where(r => r.StartDate <= sale.Date && r.EndDate >= sale.Date)
                .Include(r => r.Products)
                .ToListAsync();

            var promoNames = new Dictionary<int, string>();
            foreach (var item in sale.Items.Where(i => i.DiscountAmount > 0))
            {
                var matchingPromo = promotions.FirstOrDefault(r => r.Products.Any(p => p.ProductId == item.ProductId));
                if (matchingPromo != null)
                {
                    promoNames[item.ProductId] = matchingPromo.Name;
                }
                else
                {
                    promoNames[item.ProductId] = "Descuento Promoción";
                }
            }
            ViewBag.PromoNames = promoNames;

            ViewBag.CompanyName = _config["CompanyInfo:Name"] ?? "GestionQ";
            ViewBag.CompanyAddress = _config["CompanyInfo:Address"] ?? "";
            ViewBag.CompanyPhone = _config["CompanyInfo:Phone"] ?? "";
            ViewBag.CompanyEmail = _config["CompanyInfo:Email"] ?? "";

            return View(sale);
        }

        [HttpPost]
        public async Task<IActionResult> CancelSale(int id)
        {
            var sale = await _context.Sales
                .Include(s => s.Payments)
                .ThenInclude(p => p.PaymentMethod)
                .Include(s => s.Items)
                .ThenInclude(i => i.Product)
                .Include(s => s.ElectronicInvoice)
                .Include(s => s.PointOfSale)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null)
            {
                return Json(new { success = false, message = "Venta no encontrada." });
            }

            if (sale.IsCancelled)
            {
                return Json(new { success = false, message = "La venta ya se encuentra anulada." });
            }

            // MP Refund
            var mpPayment = sale.Payments.FirstOrDefault(p => p.PaymentMethod != null && p.PaymentMethod.Name.Contains("Mercado Pago") && !string.IsNullOrEmpty(p.TransactionReference));
            if (mpPayment != null)
            {
                var config = await _context.MercadoPagoConfigs
                    .Where(c => c.IsActive && (c.PointOfSaleId == null || c.PointOfSaleId == sale.PointOfSaleId))
                    .FirstOrDefaultAsync();

                string accessToken = config?.AccessToken;

                if (string.IsNullOrEmpty(accessToken))
                {
                    return Json(new { success = false, message = "No hay configuración de Mercado Pago para este Punto de Venta configurada en el Panel." });
                }

                var refundResult = await _mpService.RefundPaymentAsync(mpPayment.TransactionReference, accessToken);
                if (!refundResult.Success)
                {
                    return Json(new { success = false, message = "No se pudo devolver el dinero en Mercado Pago: " + refundResult.ErrorMessage });
                }
            }

            // Anular Venta
            sale.IsCancelled = true;
            sale.CancellationDate = DateTime.Now;

            // Devolver Stock
            foreach (var item in sale.Items)
            {
                if (item.Product != null)
                {
                    var previousStock = item.Product.Stock;
                    item.Product.Stock += item.Quantity;

                    _context.StockMovements.Add(new StockMovement
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Type = MovementType.Return,
                        Concept = $"Anulación Venta #{sale.FormattedTicketNumber}",
                        Date = DateTime.Now,
                        PreviousStock = previousStock,
                        NewStock = item.Product.Stock,
                        SaleId = sale.Id
                    });
                }
            }

            // Nota de Crédito AFIP si corresponde
            if (sale.ElectronicInvoice != null && sale.ElectronicInvoice.Status == "Aprobado")
            {
                // La Nota de Crédito tiene un comprobante distinto según la factura original
                int ncDocTypeCode = 0;
                switch (sale.ElectronicInvoice.InvoiceTypeCode)
                {
                    case 1: ncDocTypeCode = 3; break; // Factura A -> Nota de Crédito A
                    case 6: ncDocTypeCode = 8; break; // Factura B -> Nota de Crédito B
                    case 11: ncDocTypeCode = 13; break; // Factura C -> Nota de Crédito C
                }

                if (ncDocTypeCode != 0 && sale.PointOfSale != null)
                {
                    var invoiceRequest = new ElectronicInvoiceRequest
                    {
                        PointOfSaleId = sale.PointOfSaleId.Value,
                        PointOfSaleNumber = sale.PointOfSale.PosNumber,
                        InvoiceTypeCode = ncDocTypeCode,
                        ConceptCode = sale.ElectronicInvoice.ConceptCode,
                        DocTypeCode = sale.ElectronicInvoice.DocTypeCode,
                        DocNumber = sale.ElectronicInvoice.DocNumber ?? "0",
                        CustomerName = sale.ElectronicInvoice.CustomerName,
                        CustomerTaxCondition = sale.ElectronicInvoice.CustomerTaxCondition,
                        TotalAmount = sale.ElectronicInvoice.TotalAmount,
                        NetAmount = sale.ElectronicInvoice.NetAmount,
                        VatAmount = sale.ElectronicInvoice.VatAmount,
                        ExemptAmount = sale.ElectronicInvoice.ExemptAmount,
                        CondicionIVAReceptorId = sale.ElectronicInvoice.CondicionIVAReceptorId,
                        CanMisMonExt = sale.ElectronicInvoice.CanMisMonExt
                    };

                    try
                    {
                        var afipResponse = await _electronicInvoicingService.RequestCAEAsync(invoiceRequest);
                        
                        // Guardar la nueva NC en la BD
                        var ncInvoice = new ElectronicInvoice
                        {
                            SaleId = null, // Set to null to avoid unique constraint violation on SaleId (original invoice already has it)
                            PointOfSaleId = sale.PointOfSaleId.Value,
                            PointOfSaleNumber = invoiceRequest.PointOfSaleNumber,
                            InvoiceTypeCode = invoiceRequest.InvoiceTypeCode,
                            InvoiceTypeDesc = ncDocTypeCode == 3 ? "Nota de Crédito A" : ncDocTypeCode == 8 ? "Nota de Crédito B" : "Nota de Crédito C",
                            InvoiceNumber = afipResponse.Success ? afipResponse.InvoiceNumber : 0,
                            ConceptCode = invoiceRequest.ConceptCode,
                            DocTypeCode = invoiceRequest.DocTypeCode,
                            DocNumber = invoiceRequest.DocNumber.ToString(),
                            CustomerName = sale.ElectronicInvoice.CustomerName,
                            CustomerTaxCondition = sale.ElectronicInvoice.CustomerTaxCondition,
                            TotalAmount = invoiceRequest.TotalAmount,
                            NetAmount = invoiceRequest.NetAmount,
                            VatAmount = invoiceRequest.VatAmount,
                            ExemptAmount = invoiceRequest.ExemptAmount,
                            IssueDate = DateTime.Now,
                            Status = afipResponse.Success ? "Aprobado" : "Rechazado",
                            CAE = afipResponse.Success ? afipResponse.CAE : null,
                            CAEExpirationDate = afipResponse.Success ? afipResponse.CAEExpirationDate : DateTime.MinValue,
                            ErrorMessage = afipResponse.Success ? $"Nota de Crédito para Venta #{sale.FormattedTicketNumber}" : string.Join(", ", afipResponse.Errors),
                            CondicionIVAReceptorId = sale.ElectronicInvoice.CondicionIVAReceptorId
                        };
                        _context.ElectronicInvoices.Add(ncInvoice);
                    }
                    catch (Exception ex)
                    {
                        // Si falla AFIP igual guardamos la anulación interna pero devolvemos mensaje de error parcial
                        await _context.SaveChangesAsync();
                        return Json(new { success = true, message = "Venta anulada y dinero devuelto, pero error al emitir la Nota de Crédito en AFIP: " + ex.Message });
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Venta anulada y devuelta correctamente." });
        }
    }

    public class SaleRequest
    {
        public int? CustomerId { get; set; }
        public List<SaleRequestItem> Items { get; set; } = new();
        public List<SaleRequestPayment>? Payments { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal PaymentDiscountAmount { get; set; }
    }

    public class SaleRequestItem
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal DiscountAmount { get; set; }
    }

    public class SaleRequestPayment
    {
        public int PaymentMethodId { get; set; }
        public decimal Amount { get; set; }
        public string? TransactionReference { get; set; }
    }
}
