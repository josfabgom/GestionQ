using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionQ.Infrastructure.Data;
using GestionQ.Domain.Entities;
using GestionQ.Domain.Constants;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GestionQ.Web.Controllers
{
    [AllowAnonymous]
    public class MobileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MobileController(ApplicationDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        public IActionResult Scanner()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetProduct(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return BadRequest("Código vacío");

            var product = await _context.Products
                .Include(p => p.VatRate)
                .FirstOrDefaultAsync(p => p.Barcode == barcode || p.InternalCode.ToString() == barcode);

            if (product == null) return NotFound(new { message = "Producto no encontrado" });

            return Ok(new
            {
                id = product.Id,
                name = product.Name,
                barcode = product.Barcode,
                internalCode = product.InternalCode,
                stock = product.Stock,
                price = product.Price
            });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateStock(int id, decimal quantityChange, string concept)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound("Producto no encontrado");

            decimal previousStock = product.Stock;
            product.Stock += quantityChange;
            _context.Update(product);

            var movement = new StockMovement
            {
                Date = DateTime.Now,
                ProductId = id,
                Quantity = quantityChange,
                Type = quantityChange >= 0 ? MovementType.AdjustmentIn : MovementType.AdjustmentOut,
                Concept = string.IsNullOrWhiteSpace(concept) ? "Ajuste Móvil" : concept,
                PreviousStock = previousStock,
                NewStock = product.Stock
            };
            _context.StockMovements.Add(movement);

            await _context.SaveChangesAsync();

            return Ok(new { newStock = product.Stock, message = "Stock actualizado correctamente" });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> UpdatePrice(int id, decimal newPrice)
        {
            var product = await _context.Products.Include(p => p.PriceHistory).FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound("Producto no encontrado");

            var latestPrice = product.PriceHistory.OrderByDescending(p => p.UpdateDate).FirstOrDefault();

            // We do a simple price update. BaseCost etc will be approximated or carried over.
            decimal cost = latestPrice?.BaseCost ?? 0;
            decimal margin = latestPrice?.ProfitMargin ?? 0;
            decimal tax = latestPrice?.InternalTax ?? 0;

            product.Price = newPrice;
            if (product.IsPesable) product.SendToScale = true;
            product.NeedsLabelPrint = true;

            var priceEntry = new ProductPrice
            {
                ProductId = product.Id,
                BaseCost = cost,
                ProfitMargin = margin,
                InternalTax = tax,
                FinalPrice = newPrice,
                UpdateDate = DateTime.Now
            };

            _context.ProductPrices.Add(priceEntry);
            _context.Update(product);

            await _context.SaveChangesAsync();

            return Ok(new { newPrice = product.Price, message = "Precio actualizado correctamente" });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> MarkForLabel(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound("Producto no encontrado");

            product.NeedsLabelPrint = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Agregado a la cola de impresión de etiquetas" });
        }
    }
}
