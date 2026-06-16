using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionQ.Infrastructure.Data;
using GestionQ.Domain.Entities;
using GestionQ.Web.Models;

using GestionQ.Domain.Constants;

namespace GestionQ.Web.Controllers
{
    [Authorize(Policy = Permissions.CashRegisters.View)]
    public class CashRegistersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<CashRegistersController> _logger;

        public CashRegistersController(ApplicationDbContext context, UserManager<IdentityUser> userManager, ILogger<CashRegistersController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            bool isAdmin = User.IsInRole("Admin");

            var query = _context.CashRegisters.Include(c => c.User).AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(c => c.UserId == user.Id);
            }

            var list = await query.OrderByDescending(c => c.OpeningDate).ToListAsync();

            // Check if current user has an open register
            var openRegister = await _context.CashRegisters
                .FirstOrDefaultAsync(c => c.UserId == user.Id && c.ClosingDate == null);
            ViewBag.HasOpenRegister = openRegister != null;
            if (openRegister != null) ViewBag.OpenRegisterId = openRegister.Id;

            return View(list);
        }

        public async Task<IActionResult> Open()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var terminalPosId = Request.Cookies["TerminalPOSId"];
            if (string.IsNullOrEmpty(terminalPosId))
            {
                TempData["Message"] = "Esta PC no está configurada. Por favor vincula esta terminal a un Punto de Venta.";
                return RedirectToAction("Index", "POSConfig");
            }

            int posId = int.Parse(terminalPosId);

            // Verificar si el usuario ya tiene UNA caja abierta (en cualquier POS)
            var userOpenRegister = await _context.CashRegisters
                .FirstOrDefaultAsync(c => c.UserId == user.Id && c.ClosingDate == null);

            if (userOpenRegister != null)
            {
                TempData["Message"] = "Ya tienes una caja abierta.";
                return RedirectToAction(nameof(Index));
            }

            // Verificar si el PUNTO DE VENTA ya está siendo usado por OTRO usuario
            var posOpenRegister = await _context.CashRegisters
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.PointOfSaleId == posId && c.ClosingDate == null);

            if (posOpenRegister != null)
            {
                TempData["Message"] = $"Este Punto de Venta ya tiene una caja abierta por el usuario {posOpenRegister.User?.UserName}.";
                return RedirectToAction(nameof(Index));
            }

            var pos = await _context.PointsOfSale.FindAsync(posId);
            ViewBag.PointOfSaleName = pos?.Name ?? "Desconocido";

            return View(new CashRegister { InitialBalance = 0, PointOfSaleId = posId });
        }

        [HttpPost]
        public async Task<IActionResult> Open(CashRegister model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var terminalPosId = Request.Cookies["TerminalPOSId"];
            if (string.IsNullOrEmpty(terminalPosId)) return RedirectToAction("Index", "POSConfig");

            int posId = int.Parse(terminalPosId);

            // Re-validar antes de guardar
            var existingOpen = await _context.CashRegisters
                .AnyAsync(c => (c.UserId == user.Id || c.PointOfSaleId == posId) && c.ClosingDate == null);
            
            if (existingOpen)
            {
                TempData["Message"] = "No se puede abrir la caja: el usuario o el Punto de Venta ya tienen una sesión activa.";
                return RedirectToAction(nameof(Index));
            }

            model.UserId = user.Id;
            model.PointOfSaleId = posId;
            model.OpeningDate = DateTime.Now;

            _context.CashRegisters.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Create", "Sales");
        }

        public async Task<IActionResult> Close(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var register = await _context.CashRegisters
                .Include(c => c.Movements)
                .Include(c => c.Sales)
                    .ThenInclude(s => s.Payments)
                    .ThenInclude(p => p.PaymentMethod)
                .Include(c => c.Sales)
                    .ThenInclude(s => s.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (register == null) return NotFound();
            
            // Seguridad: solo el dueño o admin pueden cerrar
            if (register.UserId != user.Id && !User.IsInRole("Admin")) return Forbid();
            
            if (register.ClosingDate != null) 
                return RedirectToAction(nameof(Details), new { id = register.Id });

            decimal totalEfectivoVentas = register.Sales
                .SelectMany(s => s.Payments)
                .Where(p => p.PaymentMethod?.Name == "Efectivo")
                .Sum(p => p.Amount);

            decimal totalIngresos = register.Movements
                .Where(m => m.Type == "Ingreso")
                .Sum(m => m.Amount);

            decimal totalEgresos = register.Movements
                .Where(m => m.Type == "Egreso")
                .Sum(m => m.Amount);

            register.ExpectedCashBalance = register.InitialBalance + totalEfectivoVentas + totalIngresos - totalEgresos;

            // Recaudación completa por medio de pago
            var paymentSummary = register.Sales
                .SelectMany(s => s.Payments)
                .GroupBy(p => p.PaymentMethod?.Name ?? "Desconocido")
                .Select(g => new { Method = g.Key, Total = g.Sum(x => x.Amount) })
                .ToList();
            ViewBag.PaymentSummary = paymentSummary;

            // Arqueo de mercadería vendida
            var productSummary = register.Sales
                .SelectMany(s => s.Items)
                .GroupBy(i => i.Product?.Name ?? "Desconocido")
                .Select(g => new ProductSalesSummaryViewModel
                {
                    ProductName = g.Key,
                    Quantity = g.Sum(x => x.Quantity),
                    Total = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .OrderByDescending(p => p.Quantity)
                .ToList();
            ViewBag.ProductSummary = productSummary;

            return View(register);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int id, string finalCashBalance)
        {
            _logger.LogInformation("=== CLOSE POST RECEIVED === id={Id}, finalCashBalance='{Balance}'", id, finalCashBalance);
            _logger.LogInformation("Form keys: {Keys}", string.Join(", ", Request.Form.Keys));

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            try
            {
                if (!TryParseDecimal(finalCashBalance, out decimal balanceValue))
                {
                    TempData["Message"] = "El monto ingresado no es un número válido.";
                    return RedirectToAction(nameof(Close), new { id });
                }

                var register = await _context.CashRegisters
                    .Include(c => c.Movements)
                    .Include(c => c.Sales)
                        .ThenInclude(s => s.Payments)
                        .ThenInclude(p => p.PaymentMethod)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (register == null) return NotFound();
                if (register.UserId != user.Id && !User.IsInRole("Admin")) return Forbid();
                if (register.ClosingDate != null) return RedirectToAction(nameof(Details), new { id = register.Id });

                decimal totalEfectivoVentas = register.Sales
                    .SelectMany(s => s.Payments)
                    .Where(p => p.PaymentMethod?.Name == "Efectivo")
                    .Sum(p => p.Amount);

                decimal totalIngresos = register.Movements
                    .Where(m => m.Type == "Ingreso")
                    .Sum(m => m.Amount);

                decimal totalEgresos = register.Movements
                    .Where(m => m.Type == "Egreso")
                    .Sum(m => m.Amount);

                register.ExpectedCashBalance = register.InitialBalance + totalEfectivoVentas + totalIngresos - totalEgresos;
                register.FinalCashBalance = balanceValue;
                register.Difference = balanceValue - register.ExpectedCashBalance;
                register.ClosingDate = DateTime.Now;

                _context.CashRegisters.Update(register);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Details), new { id = register.Id });
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error al cerrar la caja: " + ex.Message;
                return RedirectToAction(nameof(Close), new { id });
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var register = await _context.CashRegisters
                .Include(c => c.User)
                .Include(c => c.Movements)
                .Include(c => c.Sales)
                    .ThenInclude(s => s.Payments)
                    .ThenInclude(p => p.PaymentMethod)
                .Include(c => c.Sales)
                    .ThenInclude(s => s.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (register == null) return NotFound();

            if (register.IsOpen)
            {
                decimal totalEfectivoVentas = register.Sales
                    .SelectMany(s => s.Payments)
                    .Where(p => p.PaymentMethod?.Name == "Efectivo")
                    .Sum(p => p.Amount);

                decimal totalIngresos = register.Movements
                    .Where(m => m.Type == "Ingreso")
                    .Sum(m => m.Amount);

                decimal totalEgresos = register.Movements
                    .Where(m => m.Type == "Egreso")
                    .Sum(m => m.Amount);

                register.ExpectedCashBalance = register.InitialBalance + totalEfectivoVentas + totalIngresos - totalEgresos;
            }

            var paymentSummary = register.Sales
                .SelectMany(s => s.Payments)
                .GroupBy(p => p.PaymentMethod?.Name ?? "Desconocido")
                .Select(g => new { Method = g.Key, Total = g.Sum(x => x.Amount) })
                .ToList();
            ViewBag.PaymentSummary = paymentSummary;

            var productSummary = register.Sales
                .SelectMany(s => s.Items)
                .GroupBy(i => i.Product?.Name ?? "Desconocido")
                .Select(g => new ProductSalesSummaryViewModel
                {
                    ProductName = g.Key,
                    Quantity = g.Sum(x => x.Quantity),
                    Total = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .OrderByDescending(p => p.Quantity)
                .ToList();
            ViewBag.ProductSummary = productSummary;

            return View(register);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMovement(int cashRegisterId, string type, string amount, string description)
        {
            _logger.LogInformation("=== ADD MOVEMENT POST RECEIVED === cashRegisterId={Id}, type='{Type}', amount='{Amount}', description='{Desc}'", cashRegisterId, type, amount, description);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var register = await _context.CashRegisters.FindAsync(cashRegisterId);
            if (register == null) return NotFound();

            if (register.UserId != user.Id && !User.IsInRole("Admin")) return Forbid();
            if (register.ClosingDate != null)
            {
                TempData["Message"] = "La caja ya está cerrada.";
                return RedirectToAction(nameof(Details), new { id = cashRegisterId });
            }

            if (!TryParseDecimal(amount, out decimal amountValue) || amountValue == 0)
            {
                _logger.LogWarning("AddMovement validation failed: TryParseDecimal returned false or amountValue is 0. Input was: '{Amount}'", amount);
                TempData["Message"] = "El monto debe ser un número válido distinto de cero.";
                return RedirectToAction(nameof(Details), new { id = cashRegisterId });
            }

            // Si el usuario ingresó un valor negativo (ej. -500 para egreso), tomamos el valor absoluto
            amountValue = Math.Abs(amountValue);
            _logger.LogInformation("AddMovement parsed amount value successfully: amountValue={Val}", amountValue);

            if (string.IsNullOrWhiteSpace(description))
            {
                TempData["Message"] = "La descripción/concepto es obligatoria.";
                return RedirectToAction(nameof(Details), new { id = cashRegisterId });
            }

            var movement = new CashRegisterMovement
            {
                CashRegisterId = cashRegisterId,
                Type = type, // "Ingreso" o "Egreso"
                Amount = amountValue,
                Description = description.Trim(),
                Date = DateTime.Now
            };

            _context.CashRegisterMovements.Add(movement);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Movimiento de caja registrado con éxito.";
            return RedirectToAction(nameof(Details), new { id = cashRegisterId });
        }

        private static bool TryParseDecimal(string? input, out decimal value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(input)) return false;

            // Limpieza robusta: eliminar símbolos de moneda ($), espacios y otros caracteres no numéricos excepto (- , .)
            string cleaned = System.Text.RegularExpressions.Regex.Replace(input.Trim(), @"[^\d\.,\-]", "");

            if (cleaned.Contains('.') && cleaned.Contains(','))
            {
                if (cleaned.IndexOf('.') < cleaned.IndexOf(','))
                {
                    cleaned = cleaned.Replace(".", "").Replace(",", ".");
                }
                else
                {
                    cleaned = cleaned.Replace(",", "");
                }
            }
            else if (cleaned.Contains(','))
            {
                cleaned = cleaned.Replace(",", ".");
            }

            return decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value);
        }
    }
}
