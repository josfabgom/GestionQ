using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionQ.Infrastructure.Data;
using GestionQ.Domain.Entities;
using GestionQ.Web.Models;
using GestionQ.Domain.Constants;
using ExcelDataReader;
using System.Data;
using System.IO;

namespace GestionQ.Web.Controllers
{
    [Authorize(Policy = Permissions.Products.View)]
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

        [HttpGet]
        [Authorize(Policy = Permissions.Products.Create)]
        public async Task<IActionResult> GenerateBarcode()
        {
            var random = new Random();
            string barcode = "";
            bool exists = true;
            while(exists)
            {
                // Generar EAN-13 interno (empieza con 20) + 10 digitos aleatorios = 12 digitos base
                string baseCode = "20" + random.NextInt64(1000000000L, 9999999999L).ToString();
                int sum = 0;
                for (int i = 0; i < 12; i++)
                {
                    int digit = int.Parse(baseCode[i].ToString());
                    sum += (i % 2 == 0) ? digit : digit * 3;
                }
                int checkDigit = (10 - (sum % 10)) % 10;
                barcode = baseCode + checkDigit.ToString();
                
                exists = await _context.Products.AnyAsync(p => p.Barcode == barcode);
            }
            return Ok(new { barcode });
        }

        [Authorize(Policy = Permissions.Products.Create)]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.VatRates = await _context.VatRates.Where(v => v.IsActive).ToListAsync();
            return View(new ProductViewModel());
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Products.Create)]
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
                    IsFractionable = model.IsFractionable,
                    SendToScale = model.SendToScale,
                    Price = model.Price,
                    Stock = model.Stock,
                    MinimumStock = model.MinimumStock,
                    VatRateId = model.VatRateId,
                    IsActive = model.IsActive,
                    ExpirationDays = model.ExpirationDays,
                    CreationDate = DateTime.Now,
                    NeedsLabelPrint = true
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

        [Authorize(Policy = Permissions.Products.Edit)]
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
                IsFractionable = product.IsFractionable,
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
        [Authorize(Policy = Permissions.Products.Edit)]
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
                    product.IsFractionable = model.IsFractionable;
                    product.SendToScale = model.SendToScale;
                    product.Price = model.Price;
                    product.Stock = model.Stock;
                    product.MinimumStock = model.MinimumStock;
                    product.VatRateId = model.VatRateId;
                    product.IsActive = model.IsActive;
                    product.ExpirationDays = model.ExpirationDays;
                    product.NeedsLabelPrint = true;

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
        [Authorize(Policy = Permissions.Products.Delete)]
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

        [Authorize(Policy = Permissions.Products.Edit)]
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

        [Authorize(Policy = Permissions.Products.Edit)]
        public async Task<IActionResult> Adjustment(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Products.Edit)]
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

        [Authorize(Policy = Permissions.Products.Create)]
        public IActionResult Import()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Products.Create)]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Seleccione un archivo válido.");
                return View("Import");
            }

            var tempFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp");
            if (!Directory.Exists(tempFolder)) Directory.CreateDirectory(tempFolder);

            var tempFilePath = Path.Combine(tempFolder, $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}");

            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            var model = new ImportMappingViewModel { TempFilePath = tempFilePath };

            using (var stream = System.IO.File.Open(tempFilePath, FileMode.Open, FileAccess.Read))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = true
                    }
                });

                if (result.Tables.Count > 0)
                {
                    var table = result.Tables[0];

                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        model.Mappings.Add(new ColumnMapping
                        {
                            ExcelColumnIndex = i,
                            ExcelColumnName = table.Columns[i].ColumnName,
                            SystemProperty = "" // Ignorar por defecto
                        });
                    }

                    int rowsToSample = Math.Min(3, table.Rows.Count);
                    for (int r = 0; r < rowsToSample; r++)
                    {
                        var rowValues = new List<string>();
                        for (int c = 0; c < table.Columns.Count; c++)
                        {
                            rowValues.Add(table.Rows[r][c]?.ToString() ?? "");
                        }
                        model.SampleRows.Add(rowValues);
                    }
                }
            }

            return View("Mapping", model);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Products.Create)]
        public async Task<IActionResult> ProcessImport(ImportMappingViewModel model)
        {
            if (string.IsNullOrEmpty(model.TempFilePath) || !System.IO.File.Exists(model.TempFilePath))
            {
                TempData["Error"] = "Archivo temporal no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using (var stream = System.IO.File.Open(model.TempFilePath, FileMode.Open, FileAccess.Read))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = true
                    }
                });

                if (result.Tables.Count > 0)
                {
                    var table = result.Tables[0];
                    int newProducts = 0;
                    int updatedProducts = 0;

                    var nameCol = model.Mappings.FirstOrDefault(m => m.SystemProperty == "Name");
                    var supplierCodeCol = model.Mappings.FirstOrDefault(m => m.SystemProperty == "SupplierCode");
                    var internalCodeCol = model.Mappings.FirstOrDefault(m => m.SystemProperty == "InternalCode");
                    var priceCol = model.Mappings.FirstOrDefault(m => m.SystemProperty == "Price");
                    var stockCol = model.Mappings.FirstOrDefault(m => m.SystemProperty == "Stock");
                    var barcodeCol = model.Mappings.FirstOrDefault(m => m.SystemProperty == "Barcode");

                    var existingProductsBySupplier = await _context.Products
                        .Where(p => p.SupplierCode != null && p.SupplierCode != "")
                        .ToDictionaryAsync(p => p.SupplierCode);

                    int maxInternalCode = await _context.Products.AnyAsync() ? await _context.Products.MaxAsync(p => p.InternalCode) : 0;

                    foreach (DataRow row in table.Rows)
                    {
                        string supplierCode = supplierCodeCol != null ? row[supplierCodeCol.ExcelColumnIndex]?.ToString() : null;
                        string name = nameCol != null ? row[nameCol.ExcelColumnIndex]?.ToString() : "Producto Sin Nombre";
                        
                        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(supplierCode)) continue;

                        decimal ParseDecimal(string val)
                        {
                            if (string.IsNullOrWhiteSpace(val)) return 0;
                            // Reemplazar comas por puntos y parsear invariant
                            val = val.Replace("$", "").Trim();
                            if (decimal.TryParse(val, System.Globalization.NumberStyles.Any, new System.Globalization.CultureInfo("es-AR"), out decimal parsedEs))
                                return parsedEs;
                            if (decimal.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedInv))
                                return parsedInv;
                            return 0;
                        }

                        decimal price = priceCol != null ? ParseDecimal(row[priceCol.ExcelColumnIndex]?.ToString()) : 0;
                        decimal stock = stockCol != null ? ParseDecimal(row[stockCol.ExcelColumnIndex]?.ToString()) : 0;
                        string barcode = barcodeCol != null ? row[barcodeCol.ExcelColumnIndex]?.ToString() : null;

                        Product product = null;
                        bool isNew = false;

                        if (!string.IsNullOrWhiteSpace(supplierCode) && existingProductsBySupplier.ContainsKey(supplierCode))
                        {
                            product = existingProductsBySupplier[supplierCode];
                            // Update
                            if (nameCol != null) product.Name = name;
                            if (priceCol != null) product.Price = price;
                            if (stockCol != null) product.Stock = stock;
                            if (barcodeCol != null) product.Barcode = barcode;
                            
                            _context.Update(product);
                            updatedProducts++;
                            
                            if (priceCol != null)
                            {
                                var priceEntry = new ProductPrice
                                {
                                    ProductId = product.Id,
                                    FinalPrice = product.Price,
                                    UpdateDate = DateTime.Now
                                };
                                _context.ProductPrices.Add(priceEntry);
                            }
                        }
                        else
                        {
                            // Create
                            maxInternalCode++;
                            product = new Product
                            {
                                InternalCode = maxInternalCode,
                                SupplierCode = supplierCode,
                                Name = string.IsNullOrWhiteSpace(name) ? "Desconocido" : name,
                                Price = price,
                                Stock = stock,
                                Barcode = barcode,
                                CreationDate = DateTime.Now,
                                IsActive = true,
                                NeedsLabelPrint = true
                            };
                            _context.Products.Add(product);
                            
                            // To be able to add ProductPrice we will do it after SaveChanges or just let it be.
                            // Since ProductId is generated after SaveChanges, we defer ProductPrice creation or save twice.
                            isNew = true;
                            newProducts++;
                        }
                    }
                    
                    await _context.SaveChangesAsync();
                    
                    // We need a second pass for new products prices
                    if (newProducts > 0)
                    {
                        // We fetch new products recently added in this batch.
                        // Or simply it's better to add the ProductPrice for the tracked entities.
                        var trackedNew = _context.ChangeTracker.Entries<Product>().Where(e => e.State == EntityState.Unchanged && e.Entity.PriceHistory.Count == 0).ToList();
                        foreach (var entry in trackedNew)
                        {
                            _context.ProductPrices.Add(new ProductPrice
                            {
                                ProductId = entry.Entity.Id,
                                FinalPrice = entry.Entity.Price,
                                UpdateDate = DateTime.Now
                            });
                        }
                        await _context.SaveChangesAsync();
                    }

                    TempData["Success"] = $"Importación completada. Nuevos: {newProducts}. Actualizados: {updatedProducts}.";
                }
            }

            // Clean up
            try
            {
                if (System.IO.File.Exists(model.TempFilePath))
                    System.IO.File.Delete(model.TempFilePath);
            }
            catch { }

            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
