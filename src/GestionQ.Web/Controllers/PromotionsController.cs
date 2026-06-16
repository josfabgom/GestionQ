using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionQ.Infrastructure.Data;
using GestionQ.Domain.Entities;
using GestionQ.Domain.Constants;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace GestionQ.Web.Controllers
{
    [Authorize(Policy = Permissions.Promotions.View)]
    public class PromotionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PromotionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var promotions = await _context.PromotionRules
                .Include(r => r.Products)
                .ThenInclude(p => p.Product)
                .OrderByDescending(r => r.EndDate)
                .ToListAsync();
            return View(promotions);
        }

        [Authorize(Policy = Permissions.Promotions.Create)]
        public async Task<IActionResult> Create()
        {
            ViewBag.Products = await _context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();
            return View(new PromotionRule
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(1)
            });
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Promotions.Create)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PromotionRule promotionRule, List<int> selectedProductIds)
        {
            ValidatePromotionRule(promotionRule);

            if (ModelState.IsValid)
            {
                if (selectedProductIds != null)
                {
                    foreach (var prodId in selectedProductIds)
                    {
                        promotionRule.Products.Add(new PromotionRuleProduct
                        {
                            ProductId = prodId
                        });
                    }
                }

                _context.Add(promotionRule);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Promoción creada correctamente.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Products = await _context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();
            return View(promotionRule);
        }

        [Authorize(Policy = Permissions.Promotions.Edit)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var promotionRule = await _context.PromotionRules
                .Include(r => r.Products)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (promotionRule == null) return NotFound();

            ViewBag.Products = await _context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return View(promotionRule);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Promotions.Edit)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PromotionRule promotionRule, List<int> selectedProductIds)
        {
            if (id != promotionRule.Id) return NotFound();

            ValidatePromotionRule(promotionRule);

            if (ModelState.IsValid)
            {
                var existing = await _context.PromotionRules
                    .Include(r => r.Products)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (existing == null) return NotFound();

                // Actualizar propiedades simples
                existing.Name = promotionRule.Name;
                existing.Type = promotionRule.Type;
                existing.Value = promotionRule.Value;
                existing.BuyQuantity = promotionRule.BuyQuantity;
                existing.PayQuantity = promotionRule.PayQuantity;
                existing.StartDate = promotionRule.StartDate;
                existing.EndDate = promotionRule.EndDate;
                existing.IsActive = promotionRule.IsActive;
                existing.IsStackable = promotionRule.IsStackable;

                // Limpiar relaciones anteriores y agregar nuevas
                _context.PromotionRuleProducts.RemoveRange(existing.Products);
                existing.Products.Clear();

                if (selectedProductIds != null)
                {
                    foreach (var prodId in selectedProductIds)
                    {
                        existing.Products.Add(new PromotionRuleProduct
                        {
                            PromotionRuleId = id,
                            ProductId = prodId
                        });
                    }
                }

                _context.Update(existing);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Promoción actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Products = await _context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();
            return View(promotionRule);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Promotions.Delete)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var promotionRule = await _context.PromotionRules.FindAsync(id);
            if (promotionRule != null)
            {
                _context.PromotionRules.Remove(promotionRule);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Promoción eliminada correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }

        private void ValidatePromotionRule(PromotionRule rule)
        {
            if (rule.StartDate > rule.EndDate)
            {
                ModelState.AddModelError(nameof(rule.StartDate), "La fecha de inicio no puede ser posterior a la fecha de fin.");
            }

            if (rule.Type == PromotionType.XForY)
            {
                if (!rule.BuyQuantity.HasValue || rule.BuyQuantity.Value <= 0)
                {
                    ModelState.AddModelError(nameof(rule.BuyQuantity), "La cantidad a comprar es obligatoria para promociones XxY.");
                }
                if (!rule.PayQuantity.HasValue || rule.PayQuantity.Value <= 0)
                {
                    ModelState.AddModelError(nameof(rule.PayQuantity), "La cantidad a pagar es obligatoria para promociones XxY.");
                }
                if (rule.BuyQuantity.HasValue && rule.PayQuantity.HasValue && rule.BuyQuantity.Value <= rule.PayQuantity.Value)
                {
                    ModelState.AddModelError(nameof(rule.BuyQuantity), "La cantidad a comprar debe ser mayor que la cantidad a pagar.");
                }
            }
            else if (rule.Type == PromotionType.Volume)
            {
                if (!rule.BuyQuantity.HasValue || rule.BuyQuantity.Value <= 0)
                {
                    ModelState.AddModelError(nameof(rule.BuyQuantity), "La cantidad mínima requerida es obligatoria para promociones por volumen.");
                }
                if (rule.Value <= 0)
                {
                    ModelState.AddModelError(nameof(rule.Value), "El precio unitario promocional debe ser mayor a 0.");
                }
            }
            else if (rule.Type == PromotionType.Percentage)
            {
                if (rule.Value <= 0 || rule.Value > 100)
                {
                    ModelState.AddModelError(nameof(rule.Value), "El porcentaje de descuento debe estar entre 0.01 y 100.");
                }
            }
            else if (rule.Type == PromotionType.FixedAmount)
            {
                if (rule.Value <= 0)
                {
                    ModelState.AddModelError(nameof(rule.Value), "El descuento fijo por unidad debe ser mayor a 0.");
                }
            }
        }
    }
}
