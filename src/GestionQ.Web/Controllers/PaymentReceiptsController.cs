using System;
using System.Linq;
using System.Threading.Tasks;
using GestionQ.Domain.Entities;
using GestionQ.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GestionQ.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PaymentReceiptsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public PaymentReceiptsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var receipts = await _context.PaymentReceipts
                .Include(p => p.Supplier)
                .Include(p => p.PaymentMethod)
                .Include(p => p.User)
                .OrderByDescending(p => p.Date)
                .ToListAsync();
            return View(receipts);
        }

        public IActionResult Create(int? supplierId)
        {
            ViewBag.PaymentMethods = _context.PaymentMethods.ToList();
            ViewBag.Suppliers = new SelectList(_context.Suppliers.Where(s => s.IsActive), "Id", "Name", supplierId);
            
            if (supplierId.HasValue)
            {
                var unpaidPurchases = _context.Purchases
                    .Where(p => p.SupplierId == supplierId.Value && (p.TotalAmount - p.PaidAmount) > 0 && p.Status != PurchaseStatus.Cancelled)
                    .Select(p => new {
                        Id = p.Id,
                        Display = $"Factura Nro {p.ReferenceNumber} - Saldo: ${(p.TotalAmount - p.PaidAmount):N2}"
                    }).ToList();
                ViewBag.UnpaidPurchases = new SelectList(unpaidPurchases, "Id", "Display");
                
                var supplier = _context.Suppliers.Find(supplierId.Value);
                if (supplier != null) ViewBag.SupplierBalance = supplier.Balance;
            }

            var model = new PaymentReceipt { SupplierId = supplierId };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PaymentReceipt model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            ModelState.Remove("ReceiptNumber");
            ModelState.Remove("User");
            ModelState.Remove("PaymentMethod");

            if (ModelState.IsValid)
            {
                // Generar ReceiptNumber (OP-00000X)
                var lastReceipt = await _context.PaymentReceipts.OrderByDescending(r => r.Id).FirstOrDefaultAsync();
                int nextId = (lastReceipt?.Id ?? 0) + 1;
                model.ReceiptNumber = $"OP-{nextId:D6}";
                
                model.UserId = user.Id;
                model.Date = DateTime.Now;

                // Lógica de Proveedores (si aplica)
                if (model.SupplierId.HasValue)
                {
                    var supplier = await _context.Suppliers.FindAsync(model.SupplierId.Value);
                    if (supplier != null)
                    {
                        supplier.Balance -= model.Amount; // Disminuye deuda
                        _context.Update(supplier);
                        
                        if (model.PurchaseId.HasValue)
                        {
                            var purchase = await _context.Purchases.FindAsync(model.PurchaseId.Value);
                            if (purchase != null)
                            {
                                purchase.PaidAmount += model.Amount;
                                _context.Update(purchase);
                            }
                        }
                    }
                }

                _context.PaymentReceipts.Add(model);

                // EGRESO de Caja Central
                var centralMovement = new CentralCashMovement
                {
                    Date = DateTime.Now,
                    Type = "Egreso",
                    Amount = model.Amount,
                    Concept = $"Orden de Pago {model.ReceiptNumber} - {model.Concept}",
                    UserId = user.Id,
                    PaymentReceipt = model // Vinculamos
                };
                _context.CentralCashMovements.Add(centralMovement);

                await _context.SaveChangesAsync();

                TempData["Message"] = "Orden de Pago / Egreso registrado con éxito.";
                return RedirectToAction(nameof(Print), new { id = model.Id });
            }

            ViewBag.PaymentMethods = _context.PaymentMethods.ToList();
            ViewBag.Suppliers = new SelectList(_context.Suppliers.Where(s => s.IsActive), "Id", "Name", model.SupplierId);
            return View(model);
        }

        public async Task<IActionResult> Print(int id)
        {
            var receipt = await _context.PaymentReceipts
                .Include(r => r.Supplier)
                .Include(r => r.PaymentMethod)
                .Include(r => r.User)
                .Include(r => r.Purchase)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receipt == null) return NotFound();
            return View(receipt);
        }
    }
}
