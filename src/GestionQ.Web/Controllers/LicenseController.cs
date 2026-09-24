using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Tasks;
using GestionQ.Licensing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;

namespace GestionQ.Web.Controllers
{
    [AllowAnonymous]
    public class LicenseController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public LicenseController(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }

        public IActionResult Index()
        {
            string licenseKey = _configuration["LicenseKey"] ?? string.Empty;
            bool isValid = LicenseValidator.IsLicenseValid(licenseKey, out string error);

            if (isValid)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.HardwareId = HardwareInfo.GetHardwareId();
            ViewBag.ErrorMessage = error;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(string licenseKey)
        {
            if (string.IsNullOrWhiteSpace(licenseKey))
            {
                TempData["ErrorMessage"] = "Debe ingresar una clave de licencia.";
                return RedirectToAction("Index");
            }

            bool isValid = LicenseValidator.IsLicenseValid(licenseKey, out string error);
            if (!isValid)
            {
                TempData["ErrorMessage"] = error;
                return RedirectToAction("Index");
            }

            try
            {
                var appSettingsPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
                var json = await System.IO.File.ReadAllTextAsync(appSettingsPath);
                var node = JsonNode.Parse(json);
                if (node != null)
                {
                    node["LicenseKey"] = licenseKey;
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    await System.IO.File.WriteAllTextAsync(appSettingsPath, node.ToJsonString(options));
                }

                TempData["SuccessMessage"] = "Licencia activada correctamente.";
                return RedirectToAction("Index", "Home");
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = "Error al guardar la licencia: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}
