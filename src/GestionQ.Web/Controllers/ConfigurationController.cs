using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using GestionQ.Domain.Entities;
using GestionQ.Web.Models;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;

using GestionQ.Domain.Constants;

namespace GestionQ.Web.Controllers
{
    [Authorize(Policy = Permissions.Config.View)]
    public class ConfigurationController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly GestionQ.Infrastructure.Data.ApplicationDbContext _context;

        public ConfigurationController(IWebHostEnvironment env, IConfiguration config, GestionQ.Infrastructure.Data.ApplicationDbContext context)
        {
            _env = env;
            _config = config;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult SystemSettings()
        {
            var connString = _config.GetConnectionString("DefaultConnection");
            var builder = new SqlConnectionStringBuilder(connString);

            var model = new ConfigurationViewModel
            {
                Server = builder.DataSource,
                Database = builder.InitialCatalog,
                User = builder.UserID,
                Password = builder.Password,
                CompanyName = _config["CompanyInfo:Name"] ?? "",
                CompanyFantasyName = _config["CompanyInfo:FantasyName"] ?? "",
                CompanyAddress = _config["CompanyInfo:Address"] ?? "",
                CompanyPhone = _config["CompanyInfo:Phone"] ?? "",
                CompanyEmail = _config["CompanyInfo:Email"] ?? "",
                CompanyCuit = _config["CompanyInfo:Cuit"] ?? "",
                CompanyTaxCondition = _config["CompanyInfo:TaxCondition"] ?? "",
                CompanyStartOfActivities = DateTime.TryParse(_config["CompanyInfo:StartOfActivities"], out var date) ? date : null,
                CompanyIIBB = _config["CompanyInfo:IIBB"] ?? "",
                JDataGateFolderPath = _config["Scale:JDataGateFolderPath"] ?? @"C:\JDataGate\IN\"
            };

            var setting = _context.SystemSettings.FirstOrDefault(s => s.Key == "NextInternalSupplierNumber");
            if (setting != null && int.TryParse(setting.Value, out var nextNum))
            {
                model.NextInternalSupplierNumber = nextNum;
            }
            else
            {
                model.NextInternalSupplierNumber = 1;
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> TestConnection([FromBody] ConfigurationViewModel model)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = model.Server,
                    InitialCatalog = model.Database,
                    UserID = model.User,
                    Password = model.Password,
                    TrustServerCertificate = true,
                    MultipleActiveResultSets = true,
                    ConnectTimeout = 3
                };

                using var connection = new SqlConnection(builder.ConnectionString);
                await connection.OpenAsync();
                return Ok(new { success = true, message = "Conexión exitosa con el servidor SQL." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Fallo de conexión: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SystemSettings(ConfigurationViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                // Procesar el Logo
                if (model.LogoFile != null && model.LogoFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "images");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    var filePath = Path.Combine(uploadsFolder, "logo.png");

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.LogoFile.CopyToAsync(stream);
                    }
                }

                // Generar nueva cadena de conexión
                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = model.Server,
                    InitialCatalog = model.Database,
                    UserID = model.User,
                    Password = model.Password,
                    TrustServerCertificate = true,
                    MultipleActiveResultSets = true
                };

                // Guardar en appsettings.json
                var appSettingsPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
                var json = await System.IO.File.ReadAllTextAsync(appSettingsPath);

                var node = JsonNode.Parse(json);
                if (node != null)
                {
                    if (node["ConnectionStrings"] == null)
                    {
                        node["ConnectionStrings"] = new JsonObject();
                    }
                    node["ConnectionStrings"]!["DefaultConnection"] = builder.ConnectionString;

                    if (node["CompanyInfo"] == null)
                    {
                        node["CompanyInfo"] = new JsonObject();
                    }
                    node["CompanyInfo"]!["Name"] = model.CompanyName;
                    node["CompanyInfo"]!["FantasyName"] = model.CompanyFantasyName;
                    node["CompanyInfo"]!["Address"] = model.CompanyAddress;
                    node["CompanyInfo"]!["Phone"] = model.CompanyPhone;
                    node["CompanyInfo"]!["Email"] = model.CompanyEmail;
                    node["CompanyInfo"]!["Cuit"] = model.CompanyCuit;
                    node["CompanyInfo"]!["TaxCondition"] = model.CompanyTaxCondition;
                    node["CompanyInfo"]!["StartOfActivities"] = model.CompanyStartOfActivities?.ToString("yyyy-MM-dd");
                    node["CompanyInfo"]!["IIBB"] = model.CompanyIIBB;

                    if (node["Scale"] == null)
                    {
                        node["Scale"] = new JsonObject();
                    }
                    node["Scale"]!["JDataGateFolderPath"] = model.JDataGateFolderPath;

                    var options = new JsonSerializerOptions { WriteIndented = true };
                    await System.IO.File.WriteAllTextAsync(appSettingsPath, node.ToJsonString(options));
                }

                // Guardar en Base de Datos
                var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == "NextInternalSupplierNumber");
                if (setting == null)
                {
                    setting = new SystemSetting { Key = "NextInternalSupplierNumber", Description = "Próximo número de ingreso de proveedor interno" };
                    _context.SystemSettings.Add(setting);
                }
                setting.Value = model.NextInternalSupplierNumber.ToString();
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Configuración guardada correctamente.";
                return RedirectToAction(nameof(SystemSettings));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar la configuración: " + ex.Message);
                return View(model);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCompanyIdentity(ConfigurationViewModel model)
        {
            ModelState.Remove("Server");
            ModelState.Remove("Database");
            ModelState.Remove("User");
            ModelState.Remove("Password");

            if (!ModelState.IsValid)
            {
                var connString = _config.GetConnectionString("DefaultConnection");
                var builder = new SqlConnectionStringBuilder(connString);
                model.Server = builder.DataSource;
                model.Database = builder.InitialCatalog;
                model.User = builder.UserID;
                model.Password = builder.Password;
                return View("SystemSettings", model);
            }

            try
            {
                var appSettingsPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
                var json = await System.IO.File.ReadAllTextAsync(appSettingsPath);
                var node = JsonNode.Parse(json);
                if (node != null)
                {
                    if (node["CompanyInfo"] == null)
                    {
                        node["CompanyInfo"] = new JsonObject();
                    }
                    node["CompanyInfo"]!["Name"] = model.CompanyName;
                    node["CompanyInfo"]!["FantasyName"] = model.CompanyFantasyName;
                    node["CompanyInfo"]!["Address"] = model.CompanyAddress;
                    node["CompanyInfo"]!["Phone"] = model.CompanyPhone;
                    node["CompanyInfo"]!["Email"] = model.CompanyEmail;
                    node["CompanyInfo"]!["Cuit"] = model.CompanyCuit;
                    node["CompanyInfo"]!["TaxCondition"] = model.CompanyTaxCondition;
                    node["CompanyInfo"]!["StartOfActivities"] = model.CompanyStartOfActivities?.ToString("yyyy-MM-dd");
                    node["CompanyInfo"]!["IIBB"] = model.CompanyIIBB;

                    var options = new JsonSerializerOptions { WriteIndented = true };
                    await System.IO.File.WriteAllTextAsync(appSettingsPath, node.ToJsonString(options));
                }

                TempData["SuccessMessage"] = "Identidad de la empresa actualizada correctamente.";
                return RedirectToAction(nameof(SystemSettings));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar la identidad: " + ex.Message);
                return View("SystemSettings", model);
            }
        }
    }
}
