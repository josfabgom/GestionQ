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

            var openRegister = await _context.CashRegisters
                .FirstOrDefaultAsync(c => c.UserId == user.Id && c.ClosingDate == null);

            if (openRegister != null)
            {
                TempData["Message"] = "Ya tienes una caja abierta.";
                return RedirectToAction(nameof(Index));
            }

            return View(new CashRegister { InitialBalance = 0 });
        }

        [HttpPost]
        public async Task<IActionResult> Open(CashRegister model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            model.UserId = user.Id;
            model.OpeningDate = DateTime.Now;

            _context.CashRegisters.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Close(int id)
        {
            var register = await _context.CashRegisters
                .Include(c => c.Sales)
                .ThenInclude(s => s.Payments)
                .ThenInclude(p => p.PaymentMethod)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (register == null || register.ClosingDate != null) return NotFound();

            decimal totalEfectivoVentas = register.Sales
                .SelectMany(s => s.Payments)
                .Where(p => p.PaymentMethod?.Name == "Efectivo")
                .Sum(p => p.Amount);

            register.ExpectedCashBalance = register.InitialBalance + totalEfectivoVentas;

            return View(register);
        }

        [HttpPost]
        public async Task<IActionResult> Close(int id, decimal finalCashBalance)
        {
            var register = await _context.CashRegisters
                .Include(c => c.Sales)
                .ThenInclude(s => s.Payments)
                .ThenInclude(p => p.PaymentMethod)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (register == null || register.ClosingDate != null) return NotFound();

            decimal totalEfectivoVentas = register.Sales
                .SelectMany(s => s.Payments)
                .Where(p => p.PaymentMethod?.Name == "Efectivo")
                .Sum(p => p.Amount);

            register.ExpectedCashBalance = register.InitialBalance + totalEfectivoVentas;
            register.FinalCashBalance = finalCashBalance;
            register.Difference = finalCashBalance - register.ExpectedCashBalance;
            register.ClosingDate = DateTime.Now;

            _context.CashRegisters.Update(register);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = register.Id });
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
