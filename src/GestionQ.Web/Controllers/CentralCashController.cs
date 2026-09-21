using System;
using System.Linq;
using System.Threading.Tasks;
using GestionQ.Domain.Entities;
using GestionQ.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionQ.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CentralCashController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CentralCashController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.CentralCashMovements.Include(m => m.User).AsQueryable();

            if (startDate.HasValue)
                query = query.Where(m => m.Date.Date >= startDate.Value.Date);
            if (endDate.HasValue)
                query = query.Where(m => m.Date.Date <= endDate.Value.Date);

            var movements = await query.OrderByDescending(m => m.Date).ToListAsync();

            decimal totalIngresos = await _context.CentralCashMovements.Where(m => m.Type == "Ingreso").SumAsync(m => m.Amount);
            decimal totalEgresos = await _context.CentralCashMovements.Where(m => m.Type == "Egreso").SumAsync(m => m.Amount);

            ViewBag.CurrentBalance = totalIngresos - totalEgresos;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            return View(movements);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMovement(CentralCashMovement model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (ModelState.IsValid)
            {
                model.UserId = user.Id;
                model.Date = DateTime.Now;
                _context.CentralCashMovements.Add(model);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Movimiento registrado exitosamente.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
