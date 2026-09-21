using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionQ.Domain.Entities;
using GestionQ.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using GestionQ.Domain.Constants;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GestionQ.Web.Controllers
{
    [Authorize(Policy = Permissions.Purchases.View)]
    public class SupplierPaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SupplierPaymentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var payments = await _context.SupplierPayments
                .Include(p => p.Supplier)
                .Include(p => p.PaymentMethod)
                .Include(p => p.Purchase)
                .OrderByDescending(p => p.Date)
                .ToListAsync();
            return View(payments);
        }

        [Authorize(Policy = Permissions.Purchases.Create)]
        public async Task<IActionResult> Create(int supplierId, int? purchaseId)
        {
            var supplier = await _context.Suppliers.FindAsync(supplierId);
            if (supplier == null) return NotFound();

            ViewBag.SupplierName = supplier.Name;
            ViewBag.SupplierBalance = supplier.Balance;
            ViewBag.PaymentMethods = await _context.PaymentMethods.Where(p => p.IsActive).ToListAsync();

            var unpaidPurchases = await _context.Purchases
                .Where(p => p.SupplierId == supplierId && p.Status == PurchaseStatus.Received && p.PaidAmount < p.TotalAmount)
                .OrderBy(p => p.Date)
                .Select(p => new {
                    Id = p.Id,
                    Text = $"Ref: {p.ReferenceNumber} - Pendiente: {(p.TotalAmount - p.PaidAmount).ToString("C2")}"
                })
                .ToListAsync();
            
            ViewBag.UnpaidPurchases = new SelectList(unpaidPurchases, "Id", "Text", purchaseId);

            decimal suggestedAmount = 0;
            if (purchaseId.HasValue)
            {
                var pur = await _context.Purchases.FindAsync(purchaseId.Value);
                if (pur != null) suggestedAmount = pur.TotalAmount - pur.PaidAmount;
            }

            var model = new SupplierPayment
            {
                SupplierId = supplierId,
                PurchaseId = purchaseId,
                Date = DateTime.Now,
                Amount = suggestedAmount > 0 ? suggestedAmount : 0
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Purchases.Create)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SupplierPayment payment)
        {
            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.SupplierPayments.Add(payment);

                    var supplier = await _context.Suppliers.FindAsync(payment.SupplierId);
                    if (supplier != null)
                    {
                        supplier.Balance -= payment.Amount;
                        _context.Suppliers.Update(supplier);
                    }

                    if (payment.PurchaseId.HasValue)
                    {
                        var purchase = await _context.Purchases.FindAsync(payment.PurchaseId.Value);
                        if (purchase != null)
                        {
                            purchase.PaidAmount += payment.Amount;
                            _context.Purchases.Update(purchase);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return RedirectToAction("Details", "Suppliers", new { id = payment.SupplierId });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Ocurrió un error al guardar el pago: " + ex.Message);
                }
            }

            var supp = await _context.Suppliers.FindAsync(payment.SupplierId);
            ViewBag.SupplierName = supp?.Name;
            ViewBag.SupplierBalance = supp?.Balance ?? 0;
            ViewBag.PaymentMethods = await _context.PaymentMethods.Where(p => p.IsActive).ToListAsync();
            
            var unpaidPurchases = await _context.Purchases
                .Where(p => p.SupplierId == payment.SupplierId && p.Status == PurchaseStatus.Received && p.PaidAmount < p.TotalAmount)
                .Select(p => new { Id = p.Id, Text = $"Ref: {p.ReferenceNumber} - Pendiente: {(p.TotalAmount - p.PaidAmount).ToString("C2")}" })
                .ToListAsync();
            ViewBag.UnpaidPurchases = new SelectList(unpaidPurchases, "Id", "Text", payment.PurchaseId);

            return View(payment);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Policy = Permissions.Purchases.Delete)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.SupplierPayments.FindAsync(id);
            if (payment == null) return NotFound();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var supplier = await _context.Suppliers.FindAsync(payment.SupplierId);
                if (supplier != null)
                {
                    supplier.Balance += payment.Amount;
                    _context.Suppliers.Update(supplier);
                }

                if (payment.PurchaseId.HasValue)
                {
                    var purchase = await _context.Purchases.FindAsync(payment.PurchaseId.Value);
                    if (purchase != null)
                    {
                        purchase.PaidAmount -= payment.Amount;
                        _context.Purchases.Update(purchase);
                    }
                }

                _context.SupplierPayments.Remove(payment);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction("Details", "Suppliers", new { id = payment.SupplierId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest("Error al intentar anular el pago: " + ex.Message);
            }
        }
    }
}