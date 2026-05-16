using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionQ.Infrastructure.Data;
using GestionQ.Domain.Entities;
using GestionQ.Web.Models;

using Microsoft.AspNetCore.Identity;

namespace GestionQ.Web.Controllers
{
    [Authorize]
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _config;

        public SalesController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IConfiguration config)
        {
            _context = context;
            _userManager = userManager;
            _config = config;
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

                decimal total = 0;
                var saleItems = new List<SaleItem>();

                foreach (var item in model.Items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null) return BadRequest("Producto no encontrado");
                    if (product.Stock < item.Quantity)
                        return BadRequest($"Stock insuficiente para {product.Name}");

                    var subtotal = product.Price * item.Quantity;
                    total += subtotal;

                    saleItems.Add(new SaleItem
                    {
                        ProductId = product.Id,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price
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

                var sale = new Sale
                {
                    Date = DateTime.Now,
                    CustomerId = model.CustomerId > 0 ? model.CustomerId : null,
                    TotalAmount = total,
                    UserId = user.Id,
                    CashRegisterId = openRegister.Id,
                    PointOfSaleId = openRegister.PointOfSaleId,
                    Items = saleItems,
                    Payments = model.Payments?.Select(p => new SalePayment
                    {
                        PaymentMethodId = p.PaymentMethodId,
                        Amount = p.Amount
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
                        decimal difference = total - totalPaid;
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

            ViewBag.CompanyName = _config["CompanyInfo:Name"] ?? "GestionQ";
            ViewBag.CompanyAddress = _config["CompanyInfo:Address"] ?? "";
            ViewBag.CompanyPhone = _config["CompanyInfo:Phone"] ?? "";
            ViewBag.CompanyEmail = _config["CompanyInfo:Email"] ?? "";

            return View(sale);
        }
    }

    public class SaleRequest
    {
        public int? CustomerId { get; set; }
        public List<SaleRequestItem> Items { get; set; } = new();
        public List<SaleRequestPayment>? Payments { get; set; }
    }

    public class SaleRequestItem
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
    }

    public class SaleRequestPayment
    {
        public int PaymentMethodId { get; set; }
        public decimal Amount { get; set; }
    }
}
