using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionQ.Infrastructure.Data;
using GestionQ.Domain.Entities;
using GestionQ.Web.Models;

namespace GestionQ.Web.Controllers
{
    [Authorize]
    public class CashRegistersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CashRegistersController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
                .Include(c => c.Sales)
                .ThenInclude(s => s.Payments)
                .ThenInclude(p => p.PaymentMethod)
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

            register.ExpectedCashBalance = register.InitialBalance + totalEfectivoVentas;

            return View(register);
        }

        [HttpPost]
        public async Task<IActionResult> Close(int id, string finalCashBalance)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            try 
            {
                // Limpiar el formato del número por si viene con puntos/comas de cultura
                string cleanBalance = finalCashBalance.Replace(".", "").Replace(",", ".");
                if (!decimal.TryParse(cleanBalance, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal balanceValue))
                {
                    // Intentar parseo directo si el anterior falla
                    if (!decimal.TryParse(finalCashBalance, out balanceValue))
                    {
                        TempData["Message"] = "El monto ingresado no es un número válido.";
                        return RedirectToAction(nameof(Close), new { id });
                    }
                }

                var register = await _context.CashRegisters
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

                register.ExpectedCashBalance = register.InitialBalance + totalEfectivoVentas;
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
                .Include(c => c.Sales)
                .ThenInclude(s => s.Payments)
                .ThenInclude(p => p.PaymentMethod)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (register == null) return NotFound();

            var paymentSummary = register.Sales
                .SelectMany(s => s.Payments)
                .GroupBy(p => p.PaymentMethod?.Name ?? "Desconocido")
                .Select(g => new { Method = g.Key, Total = g.Sum(x => x.Amount) })
                .ToList();

            ViewBag.PaymentSummary = paymentSummary;

            return View(register);
        }
    }
}
