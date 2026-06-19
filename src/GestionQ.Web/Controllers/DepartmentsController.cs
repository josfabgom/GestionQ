using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionQ.Infrastructure.Data;
using GestionQ.Domain.Entities;
using GestionQ.Domain.Constants;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GestionQ.Web.Controllers
{
    [Authorize(Policy = Permissions.Config.View)]
    public class DepartmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DepartmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var departments = await _context.Departments
                .Include(d => d.VatRate)
                .Include(d => d.VirtualProduct)
                .ToListAsync();
            return View(departments);
        }

        [Authorize(Policy = Permissions.Config.Create)]
        public async Task<IActionResult> Create()
        {
            ViewBag.VatRates = new SelectList(await _context.VatRates.ToListAsync(), "Id", "Rate");
            return View();
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Config.Create)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Department department)
        {
            // Remove VirtualProduct navigation validation to allow custom creation
            ModelState.Remove(nameof(department.VirtualProduct));
            ModelState.Remove(nameof(department.VatRate));

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 1. Create Virtual Product representing this department
                    int maxCode = await _context.Products.AnyAsync() 
                        ? await _context.Products.MaxAsync(p => p.InternalCode) 
                        : 0;

                    var virtualProduct = new Product
                    {
                        InternalCode = maxCode + 1,
                        Name = department.Name,
                        Price = 0,
                        Stock = 999999, // Virtual infinite stock
                        IsActive = true,
                        IsDepartment = true,
                        VatRateId = department.VatRateId,
                        CreationDate = DateTime.Now
                    };

                    _context.Products.Add(virtualProduct);
                    await _context.SaveChangesAsync();

                    // 2. Link and create Department
                    department.VirtualProductId = virtualProduct.Id;
                    _context.Departments.Add(department);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Error al crear el departamento: " + ex.Message);
                }
            }

            ViewBag.VatRates = new SelectList(await _context.VatRates.ToListAsync(), "Id", "Rate", department.VatRateId);
            return View(department);
        }

        [Authorize(Policy = Permissions.Config.Edit)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var department = await _context.Departments.FindAsync(id);
            if (department == null) return NotFound();

            ViewBag.VatRates = new SelectList(await _context.VatRates.ToListAsync(), "Id", "Rate", department.VatRateId);
            return View(department);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Config.Edit)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Department department)
        {
            if (id != department.Id) return NotFound();

            ModelState.Remove(nameof(department.VirtualProduct));
            ModelState.Remove(nameof(department.VatRate));

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Update Department
                    _context.Update(department);

                    // Find and Update Virtual Product
                    var virtualProduct = await _context.Products.FindAsync(department.VirtualProductId);
                    if (virtualProduct != null)
                    {
                        virtualProduct.Name = department.Name;
                        virtualProduct.VatRateId = department.VatRateId;
                        _context.Products.Update(virtualProduct);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Error al editar el departamento: " + ex.Message);
                }
            }

            ViewBag.VatRates = new SelectList(await _context.VatRates.ToListAsync(), "Id", "Rate", department.VatRateId);
            return View(department);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Config.Delete)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department != null)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 1. Deactivate Virtual Product to preserve sales history
                    var virtualProduct = await _context.Products.FindAsync(department.VirtualProductId);
                    if (virtualProduct != null)
                    {
                        virtualProduct.IsActive = false;
                        _context.Products.Update(virtualProduct);
                    }

                    // 2. Remove Department relation
                    _context.Departments.Remove(department);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "Error al eliminar el departamento: " + ex.Message;
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
