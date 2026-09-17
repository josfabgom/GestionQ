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

                var presentationIds = queue.Where(q => q.PresentationId.HasValue).Select(q => q.PresentationId.Value).Distinct().ToList();
                var presentations = await _context.ProductPresentations
                    .Where(p => presentationIds.Contains(p.Id))
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
                        if (item.PresentationId.HasValue && presentations.TryGetValue(item.PresentationId.Value, out var pres))
                        {
                            model.Add(new LabelPrintModel
                            {
                                Product = p,
                                Quantity = item.Quantity,
                                Format = item.Format,
                                CustomBarcode = pres.Barcode,
                                CustomPrice = pres.Price ?? (p.Price * pres.Quantity),
                                PresentationName = pres.Name
                            });
                        }
                        else
                        {
                            model.Add(new LabelPrintModel
                            {
                                Product = p,
                                Quantity = item.Quantity,
                                Format = item.Format
                            });
                        }
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
                    id = p.Id,
                    presentationId = (int?)null,
                    isPresentation = false,
                    internalCode = p.InternalCode.ToString(),
                    barcode = p.Barcode,
                    name = p.Name,
                    price = p.Price
                })
                .ToListAsync();
            
            var pendingPresentations = await _context.ProductPresentations
                .Include(p => p.Product)
                .Where(p => p.NeedsLabelPrint && p.IsActive)
                .Select(p => new {
                    id = p.ProductId,
                    presentationId = (int?)p.Id,
                    isPresentation = true,
                    internalCode = p.Product.InternalCode.ToString(),
                    barcode = p.Barcode,
                    name = $"{p.Product.Name} ({p.Name})",
                    price = p.Price ?? (p.Product.Price * p.Quantity)
                })
                .ToListAsync();

            pendingProducts.AddRange(pendingPresentations);
            return Json(pendingProducts);
        }

        public class ClearPendingRequest
        {
            public List<int> ProductIds { get; set; } = new();
            public List<int> PresentationIds { get; set; } = new();
        }

        [HttpPost]
        public async Task<IActionResult> ClearPendingStatus([FromBody] ClearPendingRequest request)
        {
            if (request == null) return Ok();

            if (request.ProductIds.Any())
            {
                var productsToClear = await _context.Products.Where(p => request.ProductIds.Contains(p.Id)).ToListAsync();
                foreach (var p in productsToClear) p.NeedsLabelPrint = false;
            }

            if (request.PresentationIds.Any())
            {
                var presToClear = await _context.ProductPresentations.Where(p => request.PresentationIds.Contains(p.Id)).ToListAsync();
                foreach (var p in presToClear) p.NeedsLabelPrint = false;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> ScaleLabels()
        {
            ViewBag.Products = await _context.Products.Where(p => p.IsActive).ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PrintScalePreview([FromForm] int productId, [FromForm] decimal amount, [FromForm] int quantity, [FromForm] string format)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return RedirectToAction(nameof(ScaleLabels));

            // Generate EAN-13
            string prefix = "2";
            string pluStr = product.InternalCode.ToString().PadLeft(6, '0');
            string amountStr;
            decimal calculatedPrice;
            string weightOrUnitText;

            if (product.IsPesable)
            {
                int grams = (int)Math.Round(amount * 1000m);
                amountStr = grams.ToString().PadLeft(5, '0');
                calculatedPrice = Math.Round(amount * product.Price, 2);
                weightOrUnitText = $"PESO: {amount:F3} kg";
            }
            else
            {
                int units = (int)amount;
                amountStr = units.ToString().PadLeft(5, '0');
                calculatedPrice = Math.Round(units * product.Price, 2);
                weightOrUnitText = $"CANT: {units} un.";
            }

            string codeWithoutCheck = prefix + pluStr + amountStr; // 12 digits
            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                int digit = int.Parse(codeWithoutCheck[i].ToString());
                sum += (i % 2 == 0) ? digit * 1 : digit * 3; // 0-indexed: Even index = odd position (x1). Odd index = even position (x3).
            }
            int checksum = (10 - (sum % 10)) % 10;
            string finalBarcode = codeWithoutCheck + checksum;

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

            var model = new List<LabelPrintModel>
            {
                new LabelPrintModel
                {
                    Product = product,
                    Quantity = quantity,
                    Format = format,
                    CustomBarcode = finalBarcode,
                    CustomPrice = calculatedPrice,
                    WeightOrUnitLabel = weightOrUnitText
                }
            };

            return View("PrintPreview", model);
        }
    }

    public class PrintQueueItem
    {
        public int ProductId { get; set; }
        public int? PresentationId { get; set; }
        public int Quantity { get; set; }
        public string Format { get; set; } // "50x35", "A4", "A4_Half"
    }

    public class LabelPrintModel
    {
        public GestionQ.Domain.Entities.Product Product { get; set; }
        public int Quantity { get; set; }
        public string Format { get; set; }
        public string CustomBarcode { get; set; }
        public decimal? CustomPrice { get; set; }
        public string WeightOrUnitLabel { get; set; }
        public string PresentationName { get; set; }
    }
}
