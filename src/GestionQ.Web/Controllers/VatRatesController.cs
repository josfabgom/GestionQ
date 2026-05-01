using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionQ.Domain.Entities;
using GestionQ.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;

namespace GestionQ.Web.Controllers
{
    [Authorize]
    public class VatRatesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VatRatesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.VatRates.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VatRate vatRate)
        {
            if (ModelState.IsValid)
            {
                _context.Add(vatRate);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(vatRate);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var vatRate = await _context.VatRates.FindAsync(id);
            if (vatRate == null) return NotFound();
            return View(vatRate);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VatRate vatRate)
        {
            if (id != vatRate.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(vatRate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VatRateExists(vatRate.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(vatRate);
        }

        private bool VatRateExists(int id)
        {
            return _context.VatRates.Any(e => e.Id == id);
        }
    }
}
