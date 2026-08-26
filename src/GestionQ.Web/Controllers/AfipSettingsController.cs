using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GestionQ.Domain.Entities;
using GestionQ.Infrastructure.Data;
using GestionQ.Infrastructure.Services;
using GestionQ.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionQ.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AfipSettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IElectronicInvoicingService _afipService;

        public AfipSettingsController(ApplicationDbContext context, IWebHostEnvironment env, IElectronicInvoicingService afipService)
        {
            _context = context;
            _env = env;
            _afipService = afipService;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _context.SystemSettings.ToDictionaryAsync(x => x.Key, x => x.Value);
            
            var vm = new AfipSettingsViewModel
            {
                Cuit = settings.GetValueOrDefault("Afip_Cuit", ""),
                Environment = settings.GetValueOrDefault("Afip_Environment", "Homologation"),
                CertificatePassword = settings.GetValueOrDefault("Afip_CertificatePassword", ""),
            };

            var certPath = settings.GetValueOrDefault("Afip_CertificatePath");
            if (!string.IsNullOrEmpty(certPath) && System.IO.File.Exists(certPath))
            {
                vm.HasCertificateConfigured = true;
                vm.ConfiguredCertificateInfo = Path.GetFileName(certPath);
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AfipSettingsViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            await SaveSetting("Afip_Cuit", vm.Cuit);
            await SaveSetting("Afip_Environment", vm.Environment);
            
            if (!string.IsNullOrEmpty(vm.CertificatePassword))
            {
                await SaveSetting("Afip_CertificatePassword", vm.CertificatePassword);
            }

            var certsFolder = Path.Combine(_env.ContentRootPath, "Certificados");
            if (!Directory.Exists(certsFolder))
            {
                Directory.CreateDirectory(certsFolder);
            }

            if (vm.CertificateFile != null)
            {
                var ext = Path.GetExtension(vm.CertificateFile.FileName).ToLower();
                var destPath = Path.Combine(certsFolder, "certificate" + ext);
                using (var stream = new FileStream(destPath, FileMode.Create))
                {
                    await vm.CertificateFile.CopyToAsync(stream);
                }
                await SaveSetting("Afip_CertificatePath", destPath);
            }

            if (vm.PrivateKeyFile != null)
            {
                var ext = Path.GetExtension(vm.PrivateKeyFile.FileName).ToLower();
                var destPath = Path.Combine(certsFolder, "private" + ext);
                using (var stream = new FileStream(destPath, FileMode.Create))
                {
                    await vm.PrivateKeyFile.CopyToAsync(stream);
                }
                await SaveSetting("Afip_PrivateKeyPath", destPath);
            }

            TempData["SuccessMessage"] = "Configuración de AFIP guardada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestConnection()
        {
            bool ok = await _afipService.CheckInfrastructureStatusAsync();
            if (ok)
            {
                TempData["SuccessMessage"] = "¡Conexión con servidores de AFIP (ARCA) exitosa!";
            }
            else
            {
                TempData["ErrorMessage"] = "No se pudo establecer conexión con los servidores de AFIP. Verifique su conexión a internet, CUIT y certificados.";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task SaveSetting(string key, string value)
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(x => x.Key == key);
            if (setting == null)
            {
                _context.SystemSettings.Add(new SystemSetting { Key = key, Value = value });
            }
            else
            {
                setting.Value = value;
            }
            await _context.SaveChangesAsync();
        }
    }
}
