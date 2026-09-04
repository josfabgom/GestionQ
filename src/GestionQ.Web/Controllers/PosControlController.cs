using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionQ.Infrastructure.Data;
using GestionQ.Domain.Entities;
using GestionQ.Web.Models;
using GestionQ.Domain.Constants;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GestionQ.Web.Controllers
{
    [Authorize(Policy = Permissions.CashRegisters.View)]
    public class PosControlController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public PosControlController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Dashboard principal: Lista todos los POS y su estado actual
        public async Task<IActionResult> Index()
        {
            var pointsOfSale = await _context.PointsOfSale
                .Include(pos => pos.CashRegisters.Where(cr => cr.ClosingDate == null))
                    .ThenInclude(cr => cr.User)
                .Include(pos => pos.CashRegisters.Where(cr => cr.ClosingDate == null))
                    .ThenInclude(cr => cr.Sales)
                        .ThenInclude(s => s.Payments).ThenInclude(p => p.PaymentMethod)
                .Include(pos => pos.CashRegisters.Where(cr => cr.ClosingDate == null))
                    .ThenInclude(cr => cr.Movements)
                .OrderBy(pos => pos.Name)
                .ToListAsync();

            var approvedSetting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == "LastApprovedProductSyncDate");
            DateTime maxDate = approvedSetting != null && DateTime.TryParse(approvedSetting.Value, out var parsed) 
                ? parsed 
                : DateTime.MinValue;

            int pendingCount = await _context.Products.CountAsync(p => p.LastModified > maxDate);

            ViewBag.PendingProductsCount = pendingCount;
            ViewBag.LastApprovedDate = maxDate != DateTime.MinValue ? maxDate.ToString("dd/MM/yyyy HH:mm") : "Nunca";
            ViewBag.LastApprovedDateObj = maxDate != DateTime.MinValue ? (DateTime?)maxDate : null;

            return View(pointsOfSale);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.CashRegisters.View)]
        public async Task<IActionResult> AuthorizeProductSync()
        {
            var syncDate = DateTime.Now;

            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == "LastApprovedProductSyncDate");
            if (setting == null)
            {
                setting = new SystemSetting { Key = "LastApprovedProductSyncDate", Value = syncDate.ToString("O") };
                _context.SystemSettings.Add(setting);
            }
            else
            {
                setting.Value = syncDate.ToString("O");
            }

            // Marcar todos los logs pendientes con la fecha de envío
            var pendingLogs = await _context.ProductChangeLogs
                .Where(l => l.DateSentToPos == null)
                .ToListAsync();

            foreach (var log in pendingLogs)
            {
                log.DateSentToPos = syncDate;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Artículos y Precios enviados. Las cajas POS se actualizarán en el próximo ciclo (máx 1 min).";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> SyncHistory()
        {
            var logs = await _context.ProductChangeLogs
                .Where(l => l.DateSentToPos != null)
                .OrderByDescending(l => l.DateSentToPos)
                .ThenByDescending(l => l.DateChanged)
                .ToListAsync();

            var historyGroups = logs
                .GroupBy(l => l.DateSentToPos.Value)
                .Select(g => new SyncHistoryGroupViewModel
                {
                    SyncDate = g.Key,
                    Logs = g.ToList()
                })
                .ToList();

            return View(historyGroups);
        }

        // Detalles de un POS específico, mostrando la caja viva y el histórico
        public async Task<IActionResult> Details(int id)
        {
            var pos = await _context.PointsOfSale
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pos == null) return NotFound();

            // Buscar si tiene caja abierta
            var activeRegister = await _context.CashRegisters
                .Include(c => c.User)
                .Include(c => c.Movements)
                .Include(c => c.Sales)
                    .ThenInclude(s => s.Payments).ThenInclude(p => p.PaymentMethod)
                .Include(c => c.Sales)
                    .ThenInclude(s => s.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.PointOfSaleId == id && c.ClosingDate == null);

            // Histórico de cajas (las últimas 30 cerradas)
            var historyRegisters = await _context.CashRegisters
                .Include(c => c.User)
                .Where(c => c.PointOfSaleId == id && c.ClosingDate != null)
                .OrderByDescending(c => c.ClosingDate)
                .Take(30)
                .ToListAsync();

            ViewBag.ActiveRegister = activeRegister;
            ViewBag.HistoryRegisters = historyRegisters;

            return View(pos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForceClose(int id)
        {
            var register = await _context.CashRegisters
                .Include(c => c.Movements)
                .Include(c => c.Sales)
                    .ThenInclude(s => s.Payments).ThenInclude(p => p.PaymentMethod)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (register == null) return NotFound();
            
            var terminalPosIdStr = Request.Cookies["TerminalPOSId"];
            if (string.IsNullOrEmpty(terminalPosIdStr) || !int.TryParse(terminalPosIdStr, out int posId) || register.PointOfSaleId != posId)
            {
                TempData["Message"] = "No puedes forzar el cierre de esta caja. Debe cerrarse desde el Punto de Venta donde se abrió.";
                return RedirectToAction(nameof(Details), new { id = register.PointOfSaleId });
            }
            if (register.ClosingDate != null)
            {
                TempData["Message"] = "Esta caja ya se encuentra cerrada.";
                return RedirectToAction(nameof(Details), new { id = register.PointOfSaleId });
            }

            decimal totalEfectivoVentas = register.Sales.Where(s => !s.IsCancelled).SelectMany(s => s.Payments)
                .Where(p => p.PaymentMethod?.Name == "Efectivo")
                .Sum(p => p.Amount);

            decimal totalIngresos = register.Movements
                .Where(m => m.Type == "Ingreso")
                .Sum(m => m.Amount);

            decimal totalEgresos = register.Movements
                .Where(m => m.Type == "Egreso")
                .Sum(m => m.Amount);

            register.ExpectedCashBalance = register.InitialBalance + totalEfectivoVentas + totalIngresos - totalEgresos;
            
            // Al ser cierre forzado, asumimos que el FinalCashBalance es igual al esperado para que no dé diferencia (o 0).
            register.FinalCashBalance = register.ExpectedCashBalance;
            register.Difference = 0;
            register.ClosingDate = DateTime.Now;

            _context.CashRegisters.Update(register);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "La caja fue cerrada de manera remota correctamente.";
            return RedirectToAction(nameof(Details), new { id = register.PointOfSaleId });
        }
    }
}



