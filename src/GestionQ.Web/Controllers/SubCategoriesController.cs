using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GestionQ.Infrastructure.Data;
using GestionQ.Domain.Entities;

namespace GestionQ.Web.Controllers
{
    [Authorize]
    public class SubCategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SubCategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.SubCategories.Include(s => s.Category).ToListAsync());
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.CategoryId = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubCategory subCategory)
        {
            if (ModelState.IsValid)
            {
                _context.Add(subCategory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.CategoryId = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", subCategory.CategoryId);
            return View(subCategory);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var subCategory = await _context.SubCategories.FindAsync(id);
            if (subCategory == null) return NotFound();
            ViewBag.CategoryId = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", subCategory.CategoryId);
            return View(subCategory);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SubCategory subCategory)
        {
            if (id != subCategory.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(subCategory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.CategoryId = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", subCategory.CategoryId);
            return View(subCategory);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var subCategory = await _context.SubCategories.Include(s => s.Products).FirstOrDefaultAsync(s => s.Id == id);
            if (subCategory != null)
            {
                if (subCategory.Products.Any())
                {
                    TempData["Error"] = "No se puede eliminar un subrubro que tiene productos asociados.";
                }
                else
                {
                    _context.SubCategories.Remove(subCategory);
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> GetByCategoryId(int categoryId)
        {
            var subCategories = await _context.SubCategories
                .Where(s => s.CategoryId == categoryId)
                .Select(s => new { s.Id, s.Name })
                .ToListAsync();
            return Json(subCategories);
        }
    }
}
