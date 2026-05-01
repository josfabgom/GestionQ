using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionQ.Domain.Entities;
using GestionQ.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;

namespace GestionQ.Web.Controllers
{
    [Authorize]
    public class SuppliersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SuppliersController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string q)
        {
            var query = _context.Suppliers.Include(s => s.TaxCondition).AsQueryable();

            if (!string.IsNullOrEmpty(q))
            {
                q = q.ToLower();
                query = query.Where(s => s.Name.ToLower().Contains(q) || 
                                       (s.TaxId != null && s.TaxId.Contains(q)));
            }

            ViewBag.SearchQuery = q;
            return View(await query.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var supplier = await _context.Suppliers
                .Include(s => s.TaxCondition)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (supplier == null) return NotFound();
            return View(supplier);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.TaxConditions = await _context.TaxConditions.Where(v => v.IsActive).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                _context.Add(supplier);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.TaxConditions = await _context.TaxConditions.Where(v => v.IsActive).ToListAsync();
            return View(supplier);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();
            ViewBag.TaxConditions = await _context.TaxConditions.Where(v => v.IsActive).ToListAsync();
            return View(supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Supplier supplier)
        {
            if (id != supplier.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(supplier);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SupplierExists(supplier.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier != null)
            {
                supplier.IsActive = false; // Soft delete
                _context.Update(supplier);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool SupplierExists(int id)
        {
            return _context.Suppliers.Any(e => e.Id == id);
        }
    }
}
