using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionQ.Infrastructure.Data;
using GestionQ.Domain.Entities;
using GestionQ.Web.Models;

namespace GestionQ.Web.Controllers
{
    [Authorize]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string q)
        {
            var query = _context.Products
                .Include(p => p.SubCategory)
                    .ThenInclude(s => s.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(q))
            {
                q = q.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(q) || 
                                       p.InternalCode.ToString().Contains(q) || 
                                       (p.Barcode != null && p.Barcode.Contains(q)));
            }

            ViewBag.SearchQuery = q;
            return View(await query.OrderByDescending(p => p.CreationDate).ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> Search(string q)
        {
            if (string.IsNullOrEmpty(q)) return Ok(new List<object>());

            var products = await _context.Products
                .Where(p => p.IsActive && (p.Name.Contains(q) || (p.Barcode != null && p.Barcode.Contains(q)) || p.InternalCode.ToString().Contains(q)))
                .Take(10)
                .Select(p => new {
                    p.Id,
                    p.Name,
                    p.Barcode,
                    p.InternalCode,
                    p.Price,
                    p.Stock,
                    p.IsPesable
                })
                .ToListAsync();

            return Ok(products);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.VatRates = await _context.VatRates.Where(v => v.IsActive).ToListAsync();
            return View(new ProductViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                var product = new Product
                {
                    Barcode = model.Barcode,
                    Name = model.Name,
                    SubCategoryId = model.SubCategoryId,
                    IsPesable = model.IsPesable,
                    SendToScale = model.SendToScale,
                    Price = model.Price,
                    Stock = model.Stock,
                    MinimumStock = model.MinimumStock,
                    VatRateId = model.VatRateId,
                    IsActive = model.IsActive,
                    ExpirationDays = model.ExpirationDays,
                    CreationDate = DateTime.Now
                };

                int maxCode = await _context.Products.AnyAsync() ? await _context.Products.MaxAsync(p => p.InternalCode) : 0;
                product.InternalCode = maxCode + 1;

                if (model.ImageFile != null)
                {
                    string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                    string fileName = $"{product.InternalCode}_{Guid.NewGuid()}{Path.GetExtension(model.ImageFile.FileName)}";
                    string filePath = Path.Combine(folder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(stream);
                    }
                    product.ImageUrl = $"/images/products/{fileName}";
                }

                _context.Add(product);
                await _context.SaveChangesAsync();

                var priceEntry = new ProductPrice
                {
                    ProductId = product.Id,
                    BaseCost = model.BaseCost,
                    ProfitMargin = model.ProfitMargin,
                    InternalTax = model.InternalTax,
                    FinalPrice = model.Price,
                    UpdateDate = DateTime.Now
                };
                _context.ProductPrices.Add(priceEntry);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.VatRates = await _context.VatRates.Where(v => v.IsActive).ToListAsync();
            
            if (model.SubCategoryId.HasValue)
            {
                var sub = await _context.SubCategories.FindAsync(model.SubCategoryId);
                if (sub != null)
                {
                    ViewBag.SelectedCategoryId = sub.CategoryId;
                    ViewBag.SubCategories = await _context.SubCategories.Where(s => s.CategoryId == sub.CategoryId).ToListAsync();
                }
            }
            return View(model);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.SubCategory)
                .Include(p => p.PriceHistory)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            var latestPrice = product.PriceHistory.OrderByDescending(p => p.UpdateDate).FirstOrDefault();

            var model = new ProductViewModel
            {
                Id = product.Id,
                InternalCode = product.InternalCode,
                Barcode = product.Barcode,
                Name = product.Name,
                SubCategoryId = product.SubCategoryId,
                IsPesable = product.IsPesable,
                SendToScale = product.SendToScale,
                BaseCost = latestPrice?.BaseCost ?? 0,
                ProfitMargin = latestPrice?.ProfitMargin ?? 0,
                InternalTax = latestPrice?.InternalTax ?? 0,
                Price = product.Price,
                Stock = product.Stock,
                MinimumStock = product.MinimumStock,
                VatRateId = product.VatRateId,
                ImageUrl = product.ImageUrl,
                IsActive = product.IsActive,
                ExpirationDays = product.ExpirationDays
            };

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.VatRates = await _context.VatRates.Where(v => v.IsActive).ToListAsync();
            
            if (product.SubCategoryId.HasValue)
            {
                var sub = await _context.SubCategories.FindAsync(product.SubCategoryId);
                if (sub != null)
                {
                    ViewBag.SelectedCategoryId = sub.CategoryId;
                    ViewBag.SubCategories = await _context.SubCategories.Where(s => s.CategoryId == sub.CategoryId).ToListAsync();
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var product = await _context.Products.FindAsync(id);
                    if (product == null) return NotFound();

                    product.InternalCode = model.InternalCode;
                    product.Barcode = model.Barcode;
                    product.Name = model.Name;
                    product.SubCategoryId = model.SubCategoryId;
                    product.IsPesable = model.IsPesable;
                    product.SendToScale = model.SendToScale;
                    product.Price = model.Price;
                    product.Stock = model.Stock;
                    product.MinimumStock = model.MinimumStock;
                    product.VatRateId = model.VatRateId;
                    product.IsActive = model.IsActive;
                    product.ExpirationDays = model.ExpirationDays;

                    var priceEntry = new ProductPrice
                    {
                        ProductId = product.Id,
                        BaseCost = model.BaseCost,
                        ProfitMargin = model.ProfitMargin,
                        InternalTax = model.InternalTax,
                        FinalPrice = model.Price,
                        UpdateDate = DateTime.Now
                    };
                    _context.ProductPrices.Add(priceEntry);

                    if (model.ImageFile != null)
                    {
                        if (!string.IsNullOrEmpty(product.ImageUrl))
                        {
                            string oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", product.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                        }

                        string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
                        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                        string fileName = $"{product.InternalCode}_{Guid.NewGuid()}{Path.GetExtension(model.ImageFile.FileName)}";
                        string filePath = Path.Combine(folder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.ImageFile.CopyToAsync(stream);
                        }
                        product.ImageUrl = $"/images/products/{fileName}";
                    }

                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(model.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.VatRates = await _context.VatRates.Where(v => v.IsActive).ToListAsync();
            
            if (model.SubCategoryId.HasValue)
            {
                var sub = await _context.SubCategories.FindAsync(model.SubCategoryId);
                if (sub != null)
                {
                    ViewBag.SelectedCategoryId = sub.CategoryId;
                    ViewBag.SubCategories = await _context.SubCategories.Where(s => s.CategoryId == sub.CategoryId).ToListAsync();
                }
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", product.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Movements(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            var movements = await _context.StockMovements
                .Where(m => m.ProductId == id)
                .OrderByDescending(m => m.Date)
                .ToListAsync();

            ViewBag.Product = product;
            return View(movements);
        }

        public async Task<IActionResult> Adjustment(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjustment(int id, decimal quantity, string concept)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            decimal previousStock = product.Stock;
            product.Stock += quantity;
            _context.Update(product);

            var movement = new StockMovement
            {
                Date = DateTime.Now,
                ProductId = id,
                Quantity = quantity,
                Type = quantity >= 0 ? MovementType.AdjustmentIn : MovementType.AdjustmentOut,
                Concept = concept ?? "Ajuste Manual",
                PreviousStock = previousStock,
                NewStock = product.Stock
            };
            _context.StockMovements.Add(movement);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> PurchaseSuggestions()
        {
            var products = await _context.Products
                .Include(p => p.SubCategory)
                    .ThenInclude(s => s.Category)
                .Where(p => p.IsActive && p.Stock <= p.MinimumStock && p.MinimumStock > 0)
                .OrderBy(p => p.Stock / p.MinimumStock) // Show most critical first
                .ToListAsync();

            return View(products);
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
