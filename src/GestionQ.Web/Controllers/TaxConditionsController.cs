using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionQ.Domain.Entities;
using GestionQ.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;

namespace GestionQ.Web.Controllers
{
    [Authorize]
    public class TaxConditionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TaxConditionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.TaxConditions.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaxCondition taxCondition)
        {
            if (ModelState.IsValid)
            {
                _context.Add(taxCondition);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(taxCondition);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var taxCondition = await _context.TaxConditions.FindAsync(id);
            if (taxCondition == null) return NotFound();
            return View(taxCondition);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TaxCondition taxCondition)
        {
            if (id != taxCondition.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(taxCondition);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TaxConditionExists(taxCondition.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(taxCondition);
        }

        private bool TaxConditionExists(int id)
        {
            return _context.TaxConditions.Any(e => e.Id == id);
        }
    }
}
