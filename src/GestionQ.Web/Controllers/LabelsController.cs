using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionQ.Infrastructure.Data;
using GestionQ.Domain.Constants;
using System.Text.Json;

namespace GestionQ.Web.Controllers
{
    [Authorize]
    public class LabelsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LabelsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Designer(string format = "50x35")
        {
            ViewBag.Format = format;
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == $"LabelTemplate_{format}");
            ViewBag.TemplateJson = setting?.Value ?? "null";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SaveTemplate([FromForm] string format, [FromForm] string templateJson)
        {
            var key = $"LabelTemplate_{format}";
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting == null)
            {
                setting = new GestionQ.Domain.Entities.SystemSetting
                {
                    Key = key,
                    Value = templateJson,
                    Description = $"Plantilla de impresión para formato {format}"
                };
                _context.SystemSettings.Add(setting);
            }
            else
            {
                setting.Value = templateJson;
                _context.SystemSettings.Update(setting);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }


        [HttpPost]
        public async Task<IActionResult> PrintPreview([FromForm] string printQueueJson)
        {
            if (string.IsNullOrEmpty(printQueueJson))
            {
                return RedirectToAction("Index");
            }

            try
            {
                var queue = JsonSerializer.Deserialize<List<PrintQueueItem>>(printQueueJson);
                var productIds = queue.Select(q => q.ProductId).Distinct().ToList();

                var products = await _context.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id);


                // Load templates
                var templateSettings = await _context.SystemSettings
                    .Where(s => s.Key.StartsWith("LabelTemplate_"))
                    .ToListAsync();
                
                var templatesDict = new Dictionary<string, string>();
                foreach (var t in templateSettings)
                {
                    var formatName = t.Key.Replace("LabelTemplate_", "");
                    templatesDict[formatName] = t.Value;
                }
                ViewBag.TemplatesDict = templatesDict;

                var model = new List<LabelPrintModel>();
                foreach (var item in queue)
                {
                    if (products.TryGetValue(item.ProductId, out var p))
                    {
                        model.Add(new LabelPrintModel
                        {
                            Product = p,
                            Quantity = item.Quantity,
                            Format = item.Format
                        });
                    }
                }

                return View(model);
            }
            catch (Exception)
            {
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingLabels()
        {
            var pendingProducts = await _context.Products
                .Where(p => p.NeedsLabelPrint && p.IsActive)
                .Select(p => new {
                    p.Id,
                    p.InternalCode,
                    p.Barcode,
                    p.Name,
                    p.Price
                })
                .ToListAsync();
            
            return Json(pendingProducts);
        }

        [HttpPost]
        public async Task<IActionResult> ClearPendingStatus([FromBody] List<int> productIds)
        {
            if (productIds == null || !productIds.Any()) return Ok();

            var productsToClear = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            foreach (var p in productsToClear)
            {
                p.NeedsLabelPrint = false;
            }

            if (productsToClear.Any())
            {
                await _context.SaveChangesAsync();
            }

            return Ok();
        }
    }

    public class PrintQueueItem
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string Format { get; set; } // "50x35", "A4", "A4_Half"
    }

    public class LabelPrintModel
    {
        public GestionQ.Domain.Entities.Product Product { get; set; }
        public int Quantity { get; set; }
        public string Format { get; set; }
    }
}
