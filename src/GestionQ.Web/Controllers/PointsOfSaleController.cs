using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionQ.Infrastructure.Data;
using GestionQ.Domain.Entities;

namespace GestionQ.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PointsOfSaleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PointsOfSaleController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.PointsOfSale.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(PointOfSale model)
        {
            if (ModelState.IsValid)
            {
                _context.PointsOfSale.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var pos = await _context.PointsOfSale.FindAsync(id);
            if (pos == null) return NotFound();
            return View(pos);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(PointOfSale model)
        {
            if (ModelState.IsValid)
            {
                _context.PointsOfSale.Update(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var pos = await _context.PointsOfSale.FindAsync(id);
            if (pos != null)
            {
                pos.IsActive = false;
                _context.PointsOfSale.Update(pos);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
