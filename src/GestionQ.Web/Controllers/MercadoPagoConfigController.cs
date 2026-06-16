using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionQ.Infrastructure.Data;
using GestionQ.Domain.Entities;
using GestionQ.Domain.Constants;
using GestionQ.Infrastructure.Services;

namespace GestionQ.Web.Controllers
{
    [Authorize(Policy = Permissions.Config.Manage)]
    public class MercadoPagoConfigController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MercadoPagoConfigController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var configs = await _context.MercadoPagoConfigs.Include(m => m.PointOfSale).ToListAsync();
            return View(configs);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.PointsOfSale = await _context.PointsOfSale.Where(p => p.IsActive).ToListAsync();
            return View(new MercadoPagoConfig());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MercadoPagoConfig model)
        {
            ModelState.Remove("PointOfSale");
            ModelState.Remove("ExternalPosId");
            ModelState.Remove("PointDeviceId");
            model.ExternalPosId ??= string.Empty;
            model.PointDeviceId ??= string.Empty;
            if (ModelState.IsValid)
            {
                _context.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.PointsOfSale = await _context.PointsOfSale.Where(p => p.IsActive).ToListAsync();
            return View(model);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var config = await _context.MercadoPagoConfigs.FindAsync(id);
            if (config == null) return NotFound();

            ViewBag.PointsOfSale = await _context.PointsOfSale.Where(p => p.IsActive).ToListAsync();
            return View(config);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MercadoPagoConfig model)
        {
            if (id != model.Id) return NotFound();

            ModelState.Remove("PointOfSale");
            ModelState.Remove("ExternalPosId");
            ModelState.Remove("PointDeviceId");
            model.ExternalPosId ??= string.Empty;
            model.PointDeviceId ??= string.Empty;
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(model);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MercadoPagoConfigExists(model.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.PointsOfSale = await _context.PointsOfSale.Where(p => p.IsActive).ToListAsync();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var config = await _context.MercadoPagoConfigs.FindAsync(id);
            if (config != null)
            {
                _context.MercadoPagoConfigs.Remove(config);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool MercadoPagoConfigExists(int id)
        {
            return _context.MercadoPagoConfigs.Any(e => e.Id == id);
        }

        public async Task<IActionResult> Transactions()
        {
            // Find all sales that were paid with Mercado Pago
            var mpSales = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.Payments)
                .ThenInclude(p => p.PaymentMethod)
                .Where(s => s.Payments.Any(p => p.PaymentMethod != null && p.PaymentMethod.Name.Contains("Mercado Pago")))
                .OrderByDescending(s => s.Date)
                .ToListAsync();

            return View(mpSales);
        }


    }
}
