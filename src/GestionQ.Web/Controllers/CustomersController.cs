using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionQ.Infrastructure.Data;
using GestionQ.Domain.Entities;
using GestionQ.Web.Models;

namespace GestionQ.Web.Controllers
{
    [Authorize]
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public CustomersController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<IActionResult> Index(string search)
        {
            var query = _context.Customers.Include(c => c.TaxCondition).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Name.Contains(search) || 
                                       c.InternalCode.Contains(search) || 
                                       c.Dni.Contains(search) ||
                                       c.Cuit.Contains(search));
            }

            return View(await query.ToListAsync());
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.TaxConditions = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(await _context.TaxConditions.Where(t => t.IsActive).ToListAsync(), "Id", "Name");
            return View(new CustomerViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Generate autoincremental internal code
                int maxCode = 0;
                var codes = await _context.Customers.Select(c => c.InternalCode).ToListAsync();
                foreach (var codeStr in codes)
                {
                    if (int.TryParse(codeStr, out int code))
                    {
                        if (code > maxCode) maxCode = code;
                    }
                }
                string newCode = (maxCode + 1).ToString("D5");

                var customer = new Customer
                {
                    Name = model.Name,
                    Email = model.Email,
                    Phone = model.Phone,
                    Dni = model.Dni,
                    Cuit = model.Cuit,
                    TaxConditionId = model.TaxConditionId,
                    Address = model.Address,
                    Locality = model.Locality,
                    IsActive = model.IsActive,
                    InternalCode = newCode,
                    Balance = model.Balance
                };

                if (model.ImageFile != null)
                {
                    customer.ImageUrl = await SaveImage(model.ImageFile);
                }

                _context.Add(customer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            var model = new CustomerViewModel
            {
                Id = customer.Id,
                Name = customer.Name,
                Email = customer.Email,
                Phone = customer.Phone,
                Dni = customer.Dni,
                Cuit = customer.Cuit,
                TaxConditionId = customer.TaxConditionId,
                Address = customer.Address,
                Locality = customer.Locality,
                IsActive = customer.IsActive,
                InternalCode = customer.InternalCode,
                ImageUrl = customer.ImageUrl,
                Balance = customer.Balance
            };

            ViewBag.TaxConditions = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(await _context.TaxConditions.Where(t => t.IsActive).ToListAsync(), "Id", "Name");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CustomerViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var customer = await _context.Customers.FindAsync(id);
                    if (customer == null) return NotFound();

                    customer.Name = model.Name;
                    customer.Email = model.Email;
                    customer.Phone = model.Phone;
                    customer.Dni = model.Dni;
                    customer.Cuit = model.Cuit;
                    customer.TaxConditionId = model.TaxConditionId;
                    customer.Address = model.Address;
                    customer.Locality = model.Locality;
                    customer.IsActive = model.IsActive;
                    customer.InternalCode = model.InternalCode;
                    customer.Balance = model.Balance;

                    if (model.ImageFile != null)
                    {
                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(customer.ImageUrl))
                        {
                            var oldPath = Path.Combine(_hostEnvironment.WebRootPath, customer.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                        }
                        customer.ImageUrl = await SaveImage(model.ImageFile);
                    }

                    _context.Update(customer);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomerExists(model.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.TaxConditions = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(await _context.TaxConditions.Where(t => t.IsActive).ToListAsync(), "Id", "Name");
            return View(model);
        }

        private async Task<string> SaveImage(IFormFile imageFile)
        {
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
            string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "customers");
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }
            return "/images/customers/" + uniqueFileName;
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                if (!string.IsNullOrEmpty(customer.ImageUrl))
                {
                    var path = Path.Combine(_hostEnvironment.WebRootPath, customer.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.Id == id);
        }
    }
}
