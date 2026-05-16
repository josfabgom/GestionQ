using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionQ.Infrastructure.Data;

namespace GestionQ.Web.Controllers
{
    [Authorize]
    public class POSConfigController : Controller
    {
        private readonly ApplicationDbContext _context;

        public POSConfigController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.PointsOfSale = await _context.PointsOfSale.Where(p => p.IsActive).ToListAsync();

            // Leer ID actual de la cookie
            var currentPosId = Request.Cookies["TerminalPOSId"];
            ViewBag.CurrentPosId = currentPosId;

            return View();
        }

        [HttpPost]
        public IActionResult Configure(int posId)
        {
            // Guardar en cookie por 1 año
            var options = new CookieOptions
            {
                Expires = DateTime.Now.AddYears(1),
                HttpOnly = true,
                IsEssential = true
            };
            Response.Cookies.Append("TerminalPOSId", posId.ToString(), options);

            TempData["Message"] = "Terminal configurada correctamente.";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult Unconfigure()
        {
            Response.Cookies.Delete("TerminalPOSId");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateMachineName(int posId)
        {
            var pos = await _context.PointsOfSale.FindAsync(posId);
            if (pos != null)
            {
                string detectedName = await GetDetectedMachineName();
                pos.MachineName = detectedName;
                _context.PointsOfSale.Update(pos);
                await _context.SaveChangesAsync();
                TempData["Message"] = $"Se ha actualizado el nombre de la PC para '{pos.Name}' a: {detectedName}";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> DetectMachineName()
        {
            string name = await GetDetectedMachineName();
            return Json(new { name });
        }

        private async Task<string> GetDetectedMachineName()
        {
            try
            {
                var remoteIp = HttpContext.Connection.RemoteIpAddress;
                if (remoteIp == null) return "Desconocido";

                string ip = remoteIp.ToString();
                if (ip == "::1" || ip == "127.0.0.1") return Environment.MachineName;

                var hostEntry = await System.Net.Dns.GetHostEntryAsync(ip);
                return hostEntry.HostName;
            }
            catch
            {
                return "PC-" + HttpContext.Connection.RemoteIpAddress?.ToString().Replace(".", "-");
            }
        }
    }
}
