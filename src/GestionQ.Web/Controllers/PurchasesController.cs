using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionQ.Domain.Entities;
using GestionQ.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;

using GestionQ.Domain.Constants;

namespace GestionQ.Web.Controllers
{
    [Authorize(Policy = Permissions.Purchases.View)]
    public class PurchasesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PurchasesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var purchases = await _context.Purchases
                .Include(p => p.Supplier)
                .OrderByDescending(p => p.Date)
                .ToListAsync();
            return View(purchases);
        }
        [Authorize(Policy = Permissions.Purchases.Create)]

        public async Task<IActionResult> Create()
        {
            ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive).ToListAsync();
            return View();
        }
        [HttpPost]
        [Authorize(Policy = Permissions.Purchases.Create)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] Models.PurchaseViewModel vm)
        {
            var items = System.Text.Json.JsonSerializer.Deserialize<List<PurchaseItem>>(vm.ItemsJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (items == null || !items.Any())
            {
                return Json(new { success = false, message = "La compra no tiene items" });
            }

            // Validar duplicado de comprobante para el mismo proveedor
            if (!string.IsNullOrEmpty(vm.ReferenceNumber))
            {
                bool exists = await _context.Purchases.AnyAsync(p => 
                    p.SupplierId == vm.SupplierId && 
                    p.ReferenceNumber == vm.ReferenceNumber);
                
                if (exists)
                {
                    return Json(new { success = false, message = $"El comprobante Nro {vm.ReferenceNumber} ya existe para este proveedor." });
                }
            }
            else
            {
                // Generar número interno
                var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == "NextInternalSupplierNumber");
                int nextNum = 1;
                if (setting != null && int.TryParse(setting.Value, out var num))
                {
                    nextNum = num;
                }
                
                vm.ReferenceNumber = "INT-" + nextNum.ToString("D8");
                
                // Actualizar configuración
                if (setting == null)
                {
                    _context.SystemSettings.Add(new SystemSetting { Key = "NextInternalSupplierNumber", Value = (nextNum + 1).ToString() });
                }
                else
                {
                    setting.Value = (nextNum + 1).ToString();
                }
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var purchase = new Purchase
                {
                    SupplierId = vm.SupplierId,
                    ReferenceNumber = vm.ReferenceNumber,
                    VoucherLetter = vm.VoucherLetter,
                    Notes = vm.Notes,
                    Date = DateTime.Now,
                    TotalAmount = items.Sum(i => i.Quantity * i.UnitCost),
                    Status = PurchaseStatus.Pending
                };

                if (vm.ImageFile != null)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "purchases");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + vm.ImageFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await vm.ImageFile.CopyToAsync(fileStream);
                    }
                    purchase.ImageUrl = "/uploads/purchases/" + uniqueFileName;
                }

                _context.Purchases.Add(purchase);
                await _context.SaveChangesAsync();

                foreach (var item in items)
                {
                    item.PurchaseId = purchase.Id;
                    _context.PurchaseItems.Add(item);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, purchaseId = purchase.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var purchase = await _context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (purchase == null) return NotFound();
            return View(purchase);
        }

        public async Task<IActionResult> Receive(int id)
        {
            var purchase = await _context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (purchase == null || purchase.Status != PurchaseStatus.Pending) return NotFound();
            return View(purchase);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Receive(int id, List<PurchaseItemUpdateModel> receivedItems)
        {
            var purchase = await _context.Purchases
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (purchase == null || purchase.Status != PurchaseStatus.Pending) return NotFound();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var itemUpdate in receivedItems)
                {
                    var item = purchase.Items.FirstOrDefault(i => i.Id == itemUpdate.Id);
                    if (item != null)
                    {
                        item.ReceivedQuantity = itemUpdate.ReceivedQuantity;
                        
                        var product = await _context.Products.Include(p => p.VatRate).FirstOrDefaultAsync(p => p.Id == item.ProductId);
                        if (product != null)
                        {
                            decimal previousStock = product.Stock;
                            product.Stock += itemUpdate.ReceivedQuantity;

                            // Update Price logic (keep consistent with Create)
                            decimal vatMultiplier = 1 + ((product.VatRate?.Rate ?? 0) / 100);
                            var latestPrice = await _context.ProductPrices
                                .Where(pp => pp.ProductId == product.Id)
                                .OrderByDescending(pp => pp.UpdateDate)
                                .FirstOrDefaultAsync();

                            decimal margin = latestPrice?.ProfitMargin ?? 0;
                            decimal internalTax = latestPrice?.InternalTax ?? 0;
                            decimal profitMultiplier = 1 + (margin / 100);
                            product.Price = (item.UnitCost * vatMultiplier * profitMultiplier) + internalTax;

                            _context.ProductPrices.Add(new ProductPrice
                            {
                                ProductId = product.Id,
                                BaseCost = item.UnitCost,
                                ProfitMargin = margin,
                                InternalTax = internalTax,
                                FinalPrice = product.Price,
                                UpdateDate = DateTime.Now
                            });

                            _context.StockMovements.Add(new StockMovement
                            {
                                Date = DateTime.Now,
                                ProductId = item.ProductId,
                                Quantity = itemUpdate.ReceivedQuantity,
                                Type = MovementType.Purchase,
                                Concept = $"Recepción Compra #{purchase.Id} - Ref: {purchase.ReferenceNumber}",
                                PurchaseId = purchase.Id,
                                PreviousStock = previousStock,
                                NewStock = product.Stock
                            });
                        }
                    }
                }

                purchase.Status = PurchaseStatus.Received;
                purchase.Date = DateTime.Now; // Update date to arrival date
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction(nameof(Details), new { id = id });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var purchase = await _context.Purchases
                .Include(p => p.Items)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (purchase == null) return NotFound();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // If it was received, we must reverse the stock
                if (purchase.Status == PurchaseStatus.Received)
                {
                    foreach (var item in purchase.Items)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null)
                        {
                            decimal qtyToReverse = item.ReceivedQuantity ?? item.Quantity;
                            
                            var movement = new StockMovement
                            {
                                ProductId = product.Id,
                                Quantity = -qtyToReverse,
                                Type = MovementType.AdjustmentOut,
                                Concept = $"Anulación Compra #{purchase.Id} ({purchase.ReferenceNumber})",
                                Date = DateTime.Now,
                                PreviousStock = product.Stock,
                                NewStock = product.Stock - qtyToReverse
                            };

                            product.Stock -= qtyToReverse;
                            _context.StockMovements.Add(movement);
                        }
                    }
                }

                _context.Purchases.Remove(purchase);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return BadRequest("Error al intentar anular la compra. Verifique que no existan dependencias.");
            }
        }

        private bool PurchaseExists(int id)
        {
            return _context.Purchases.Any(e => e.Id == id);
        }
    }

    public class PurchaseItemUpdateModel
    {
        public int Id { get; set; }
        public decimal ReceivedQuantity { get; set; }
    }
}
