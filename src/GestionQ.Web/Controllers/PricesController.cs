using GestionQ.Domain.Entities;
using GestionQ.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using GestionQ.Domain.Constants;

namespace GestionQ.Web.Controllers
{
    [Authorize(Policy = Permissions.Config.View)]
    public class PricesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PricesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string q, int? categoryId, int? subCategoryId)
        {
            var query = _context.Products
                .Include(p => p.SubCategory)
                    .ThenInclude(s => s.Category)
                .Include(p => p.VatRate)
                .Include(p => p.PriceHistory)
                .AsQueryable();

            if (!string.IsNullOrEmpty(q))
            {
                q = q.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(q) || p.InternalCode.ToString().Contains(q));
            }

            if (categoryId.HasValue)
                query = query.Where(p => p.SubCategory.CategoryId == categoryId);
            
            if (subCategoryId.HasValue)
                query = query.Where(p => p.SubCategoryId == subCategoryId);

            var products = await query.ToListAsync();
            
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.VatRates = await _context.VatRates.ToListAsync();
            
            return View(products);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSingle(int productId, string cost, string margin, string tax, int? vatRateId)
        {
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            decimal dCost = decimal.TryParse(cost, culture, out var c) ? c : 0;
            decimal dMargin = decimal.TryParse(margin, culture, out var m) ? m : 0;
            decimal dTax = decimal.TryParse(tax, culture, out var t) ? t : 0;

            var product = await _context.Products.Include(p => p.VatRate).FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null) return NotFound();

            if (vatRateId.HasValue)
            {
                product.VatRateId = vatRateId;
                // Reload vat rate if changed
                product.VatRate = await _context.VatRates.FindAsync(vatRateId);
            }

            decimal vatRateValue = product.VatRate?.Rate ?? 0;
            decimal finalPrice = (dCost * (1 + (vatRateValue / 100))) * (1 + (dMargin / 100)) + dTax;

            product.Price = finalPrice;
            if (product.IsPesable) product.SendToScale = true;

            var priceEntry = new ProductPrice
            {
                ProductId = productId,
                BaseCost = dCost,
                ProfitMargin = dMargin,
                InternalTax = dTax,
                FinalPrice = finalPrice,
                UpdateDate = DateTime.Now
            };

            _context.ProductPrices.Add(priceEntry);
            _context.Update(product);
            await _context.SaveChangesAsync();

            return Ok(new { finalPrice = finalPrice.ToString("F2", culture) });
        }

        [HttpPost]
        public async Task<IActionResult> BulkUpdate(int? categoryId, int? subCategoryId, decimal? percentage, string target)
        {
            if (!percentage.HasValue || percentage == 0) return RedirectToAction(nameof(Index));

            var query = _context.Products.Include(p => p.VatRate).Include(p => p.PriceHistory).AsQueryable();

            if (subCategoryId.HasValue)
                query = query.Where(p => p.SubCategoryId == subCategoryId);
            else if (categoryId.HasValue)
                query = query.Where(p => p.SubCategory.CategoryId == categoryId);

            var products = await query.ToListAsync();
            decimal multiplier = 1 + (percentage.Value / 100);

            foreach (var product in products)
            {
                var latest = product.PriceHistory.OrderByDescending(ph => ph.UpdateDate).FirstOrDefault();
                
                decimal cost = latest?.BaseCost ?? 0;
                decimal margin = latest?.ProfitMargin ?? 0;
                decimal tax = latest?.InternalTax ?? 0;
                decimal vatRate = product.VatRate?.Rate ?? 0;

                if (target == "cost")
                {
                    cost *= multiplier;
                }
                else if (target == "margin")
                {
                    margin += percentage.Value; // For margin, we usually add points or multiply? User said "actualizar masivo de precio de costo o precio final". I'll assume they want to increase the final price by X% or cost by X%.
                }
                else if (target == "final")
                {
                    // To increase final price by X%, we can adjust margin accordingly? 
                    // No, usually "bulk update final price" means Price *= multiplier.
                    // But to keep structure consistent, I'll update the final price and record it.
                }

                if (target == "final")
                {
                    product.Price *= multiplier;
                }
                else 
                {
                    product.Price = (cost * (1 + (vatRate / 100))) * (1 + (margin / 100)) + tax;
                }

                if (product.IsPesable) product.SendToScale = true;

                var priceEntry = new ProductPrice
                {
                    ProductId = product.Id,
                    BaseCost = cost,
                    ProfitMargin = margin,
                    InternalTax = tax,
                    FinalPrice = product.Price,
                    UpdateDate = DateTime.Now
                };
                _context.ProductPrices.Add(priceEntry);
                _context.Update(product);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Se actualizaron {products.Count} productos.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> History(int id)
        {
            var product = await _context.Products
                .Include(p => p.PriceHistory)
                .FirstOrDefaultAsync(p => p.Id == id);
                
            if (product == null) return NotFound();

            return View(product);
        }
    }
}
