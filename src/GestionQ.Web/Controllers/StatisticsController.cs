using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionQ.Infrastructure.Data;
using GestionQ.Web.Models;
using Microsoft.AspNetCore.Authorization;
using GestionQ.Domain.Constants;
using ClosedXML.Excel;

namespace GestionQ.Web.Controllers
{
    [Authorize(Policy = Permissions.Statistics.View)]
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StatisticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            // Set default dates if not provided
            var start = startDate ?? DateTime.Today;
            var end = endDate ?? DateTime.Today;
            
            // Adjust end date to include the whole day (up to 23:59:59)
            var endAdjusted = end.Date.AddDays(1).AddTicks(-1);

            var viewModel = new StatisticsViewModel
            {
                StartDate = start,
                EndDate = end
            };

            // Query sales within the date range that are not cancelled
            var salesQuery = _context.Sales
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.SubCategory)
                            .ThenInclude(sc => sc.Category)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.PriceHistory)
                .Include(s => s.Payments)
                .ThenInclude(p => p.PaymentMethod)
                .Where(s => s.Date >= start && s.Date <= endAdjusted && !s.IsCancelled);

            var sales = await salesQuery.ToListAsync();

            viewModel.TotalSalesAmount = sales.Sum(s => s.TotalAmount);
            viewModel.TotalItemsSold = sales.SelectMany(s => s.Items).Sum(i => i.Quantity);
            
            // Calculate Cost and Profit
            viewModel.TotalCostAmount = sales.SelectMany(s => s.Items).Sum(i => (i.Product?.PriceHistory?.OrderByDescending(ph => ph.UpdateDate).FirstOrDefault()?.BaseCost ?? 0m) * i.Quantity);
            viewModel.TotalProfitAmount = viewModel.TotalSalesAmount - viewModel.TotalCostAmount;
            if (viewModel.TotalSalesAmount > 0)
            {
                viewModel.ProfitMarginPercentage = (viewModel.TotalProfitAmount / viewModel.TotalSalesAmount) * 100m;
            }

            // Group by Product
            viewModel.SalesByProduct = sales
                .SelectMany(s => s.Items)
                .GroupBy(i => i.ProductId)
                .Select(g => new ProductSaleStat
                {
                    ProductId = g.Key,
                    ProductName = g.First().Product?.Name ?? g.First().CustomName ?? "Producto Desconocido",
                    TotalQuantity = g.Sum(i => i.Quantity),
                    TotalAmount = g.Sum(i => i.Quantity * i.UnitPrice - i.DiscountAmount) // Base price minus discount
                })
                .OrderByDescending(p => p.TotalQuantity)
                .ToList();

            // Group by Payment Method
            viewModel.SalesByPaymentMethod = sales
                .SelectMany(s => s.Payments)
                .GroupBy(p => p.PaymentMethodId)
                .Select(g => new PaymentMethodStat
                {
                    PaymentMethodId = g.Key,
                    PaymentMethodName = g.First().PaymentMethod?.Name ?? "Medio de Pago Desconocido",
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .OrderByDescending(p => p.TotalAmount)
                .ToList();

            // Group by Hour
            viewModel.SalesByHour = sales
                .GroupBy(s => s.Date.Hour)
                .Select(g => new HourlySaleStat
                {
                    Hour = g.Key,
                    TotalAmount = g.Sum(s => s.TotalAmount)
                })
                .OrderBy(h => h.Hour)
                .ToList();

            // Group by Category
            viewModel.SalesByCategory = sales
                .SelectMany(s => s.Items)
                .GroupBy(i => i.Product?.SubCategory?.Category?.Name ?? "Sin Categoría")
                .Select(g => new CategorySaleStat
                {
                    CategoryName = g.Key,
                    TotalAmount = g.Sum(i => i.Quantity * i.UnitPrice - i.DiscountAmount)
                })
                .OrderByDescending(c => c.TotalAmount)
                .ToList();

            return View(viewModel);
        }

        public async Task<IActionResult> ProductChanges(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today;
            var end = endDate ?? DateTime.Today;
            var endAdjusted = end.Date.AddDays(1).AddTicks(-1);

            var viewModel = new ProductChangesViewModel
            {
                StartDate = start,
                EndDate = end
            };

            // Products that had price changes
            var productsWithPriceChanges = await _context.ProductPrices
                .Where(p => p.UpdateDate >= start && p.UpdateDate <= endAdjusted)
                .Select(p => p.ProductId)
                .Distinct()
                .ToListAsync();

            // Products that had stock changes
            var productsWithStockChanges = await _context.StockMovements
                .Where(s => s.Date >= start && s.Date <= endAdjusted)
                .Select(s => s.ProductId)
                .Distinct()
                .ToListAsync();

            // Products newly created
            var newlyCreatedProducts = await _context.Products
                .Where(p => p.CreationDate >= start && p.CreationDate <= endAdjusted)
                .Select(p => p.Id)
                .Distinct()
                .ToListAsync();

            var allChangedProductIds = productsWithPriceChanges
                .Union(productsWithStockChanges)
                .Union(newlyCreatedProducts)
                .Distinct()
                .ToList();

            if (allChangedProductIds.Any())
            {
                var changedProducts = await _context.Products
                    .Where(p => allChangedProductIds.Contains(p.Id))
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                foreach (var p in changedProducts)
                {
                    viewModel.Changes.Add(new ProductChangeItem
                    {
                        ProductId = p.Id,
                        InternalCode = p.InternalCode,
                        Barcode = p.Barcode,
                        Name = p.Name,
                        Stock = p.Stock,
                        FinalPrice = p.Price
                    });
                }
            }

            return View(viewModel);
        }

        public async Task<IActionResult> ExportProductChanges(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today;
            var end = endDate ?? DateTime.Today;
            var endAdjusted = end.Date.AddDays(1).AddTicks(-1);

            // Products that had price changes
            var productsWithPriceChanges = await _context.ProductPrices
                .Where(p => p.UpdateDate >= start && p.UpdateDate <= endAdjusted)
                .Select(p => p.ProductId)
                .Distinct()
                .ToListAsync();

            // Products that had stock changes
            var productsWithStockChanges = await _context.StockMovements
                .Where(s => s.Date >= start && s.Date <= endAdjusted)
                .Select(s => s.ProductId)
                .Distinct()
                .ToListAsync();

            // Products newly created
            var newlyCreatedProducts = await _context.Products
                .Where(p => p.CreationDate >= start && p.CreationDate <= endAdjusted)
                .Select(p => p.Id)
                .Distinct()
                .ToListAsync();

            var allChangedProductIds = productsWithPriceChanges
                .Union(productsWithStockChanges)
                .Union(newlyCreatedProducts)
                .Distinct()
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Codigo Interno;Codigo de Barras;Producto;Stock Actual;Precio Final");

            if (allChangedProductIds.Any())
            {
                var changedProducts = await _context.Products
                    .Where(p => allChangedProductIds.Contains(p.Id))
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                foreach (var p in changedProducts)
                {
                    string name = p.Name?.Replace(";", ",") ?? "";
                    
                    sb.AppendLine($"{p.InternalCode};{p.Barcode};{name};{p.Stock};{p.Price}");
                }
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var bom = Encoding.UTF8.GetPreamble();
            var content = new byte[bom.Length + bytes.Length];
            Buffer.BlockCopy(bom, 0, content, 0, bom.Length);
            Buffer.BlockCopy(bytes, 0, content, bom.Length, bytes.Length);

            string fileName = $"Cambios_Productos_{start:yyyyMMdd}_al_{end:yyyyMMdd}.csv";
            return File(content, "text/csv", fileName);
        }

        public async Task<IActionResult> DailySalesByProduct(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today;
            var end = endDate ?? DateTime.Today;
            var endAdjusted = end.Date.AddDays(1).AddTicks(-1);

            var viewModel = new StatisticsViewModel
            {
                StartDate = start,
                EndDate = end
            };

            var salesQuery = _context.Sales
                .Include(s => s.Items)
                .ThenInclude(i => i.Product)
                .Where(s => s.Date >= start && s.Date <= endAdjusted && !s.IsCancelled);

            var sales = await salesQuery.ToListAsync();

            viewModel.SalesByProduct = sales
                .SelectMany(s => s.Items)
                .GroupBy(i => i.ProductId)
                .Select(g => new ProductSaleStat
                {
                    ProductId = g.Key,
                    ProductName = g.First().Product?.Name ?? g.First().CustomName ?? "Producto Desconocido",
                    TotalQuantity = g.Sum(i => i.Quantity),
                    TotalAmount = g.Sum(i => i.Quantity * i.UnitPrice - i.DiscountAmount)
                })
                .OrderByDescending(p => p.TotalQuantity) // de mayor a menor por cantidad (o TotalAmount?) 
                .ToList();

            return View(viewModel);
        }

        public async Task<IActionResult> ExportProfitabilityExcel(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today;
            var end = endDate ?? DateTime.Today;
            var endAdjusted = end.Date.AddDays(1).AddTicks(-1);

            var salesQuery = _context.Sales
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.SubCategory)
                            .ThenInclude(sc => sc.Category)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.PriceHistory)
                .Where(s => s.Date >= start && s.Date <= endAdjusted && !s.IsCancelled);

            var sales = await salesQuery.ToListAsync();

            var stats = sales
                .SelectMany(s => s.Items)
                .GroupBy(i => i.ProductId)
                .Select(g => new
                {
                    Codigo = g.First().Product?.Barcode ?? "",
                    Producto = g.First().Product?.Name ?? g.First().CustomName ?? "Desconocido",
                    Categoria = g.First().Product?.SubCategory?.Category?.Name ?? "",
                    CantidadVendida = g.Sum(i => i.Quantity),
                    CostoUnitario = g.First().Product?.PriceHistory?.OrderByDescending(ph => ph.UpdateDate).FirstOrDefault()?.BaseCost ?? 0m,
                    CostoTotal = g.Sum(i => (i.Product?.PriceHistory?.OrderByDescending(ph => ph.UpdateDate).FirstOrDefault()?.BaseCost ?? 0m) * i.Quantity),
                    TotalRecaudado = g.Sum(i => i.Quantity * i.UnitPrice - i.DiscountAmount),
                    GananciaNeta = g.Sum(i => i.Quantity * i.UnitPrice - i.DiscountAmount) - g.Sum(i => (i.Product?.PriceHistory?.OrderByDescending(ph => ph.UpdateDate).FirstOrDefault()?.BaseCost ?? 0m) * i.Quantity)
                })
                .OrderByDescending(p => p.TotalRecaudado)
                .ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Rentabilidad");
            var currentRow = 1;

            worksheet.Cell(currentRow, 1).Value = "Código";
            worksheet.Cell(currentRow, 2).Value = "Producto";
            worksheet.Cell(currentRow, 3).Value = "Categoría";
            worksheet.Cell(currentRow, 4).Value = "Cantidad Vendida";
            worksheet.Cell(currentRow, 5).Value = "Costo Unitario";
            worksheet.Cell(currentRow, 6).Value = "Costo Total";
            worksheet.Cell(currentRow, 7).Value = "Total Recaudado";
            worksheet.Cell(currentRow, 8).Value = "Ganancia Neta";
            worksheet.Range(1, 1, 1, 8).Style.Font.Bold = true;

            foreach (var item in stats)
            {
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = item.Codigo;
                worksheet.Cell(currentRow, 2).Value = item.Producto;
                worksheet.Cell(currentRow, 3).Value = item.Categoria;
                worksheet.Cell(currentRow, 4).Value = item.CantidadVendida;
                worksheet.Cell(currentRow, 5).Value = item.CostoUnitario;
                worksheet.Cell(currentRow, 6).Value = item.CostoTotal;
                worksheet.Cell(currentRow, 7).Value = item.TotalRecaudado;
                worksheet.Cell(currentRow, 8).Value = item.GananciaNeta;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Rentabilidad_{start:yyyyMMdd}_{end:yyyyMMdd}.xlsx");
        }

        public async Task<IActionResult> ExportLowStockExcel()
        {
            var products = await _context.Products
                .Include(p => p.PriceHistory).Include(p => p.SubCategory).ThenInclude(sc => sc.Category)
                .Where(p => p.IsActive && p.Stock <= p.MinimumStock)
                .OrderBy(p => p.SubCategory.Category.Name)
                .ThenBy(p => p.Name)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Alerta Stock");
            var currentRow = 1;

            worksheet.Cell(currentRow, 1).Value = "Código";
            worksheet.Cell(currentRow, 2).Value = "Producto";
            worksheet.Cell(currentRow, 3).Value = "Categoría";
            worksheet.Cell(currentRow, 4).Value = "Stock Actual";
            worksheet.Cell(currentRow, 5).Value = "Stock Mínimo";
            worksheet.Range(1, 1, 1, 5).Style.Font.Bold = true;

            foreach (var item in products)
            {
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = item.Barcode;
                worksheet.Cell(currentRow, 2).Value = item.Name;
                worksheet.Cell(currentRow, 3).Value = item.SubCategory?.Category?.Name ?? "";
                worksheet.Cell(currentRow, 4).Value = item.Stock;
                worksheet.Cell(currentRow, 5).Value = item.MinimumStock;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"AlertaStock_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        public async Task<IActionResult> ExportStagnantExcel(int days = 30)
        {
            var cutoffDate = DateTime.Now.AddDays(-days);

            // Get products that haven't been sold since cutoffDate
            var activeProducts = await _context.Products
                .Include(p => p.PriceHistory).Include(p => p.SubCategory).ThenInclude(sc => sc.Category)
                .Where(p => p.IsActive && p.Stock > 0)
                .ToListAsync();

            var recentSalesProductIds = await _context.Sales
                .Where(s => s.Date >= cutoffDate && !s.IsCancelled)
                .SelectMany(s => s.Items)
                .Select(i => i.ProductId)
                .Distinct()
                .ToListAsync();

            var stagnant = activeProducts
                .Where(p => !recentSalesProductIds.Contains(p.Id))
                .OrderByDescending(p => p.Stock * (p.PriceHistory?.OrderByDescending(ph => ph.UpdateDate).FirstOrDefault()?.BaseCost ?? 0m))
                .ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Baja Rotación");
            var currentRow = 1;

            worksheet.Cell(currentRow, 1).Value = "Código";
            worksheet.Cell(currentRow, 2).Value = "Producto";
            worksheet.Cell(currentRow, 3).Value = "Categoría";
            worksheet.Cell(currentRow, 4).Value = "Stock Estancado";
            worksheet.Cell(currentRow, 5).Value = "Costo Unitario";
            worksheet.Cell(currentRow, 6).Value = "Capital Inmovilizado";
            worksheet.Range(1, 1, 1, 6).Style.Font.Bold = true;

            foreach (var item in stagnant)
            {
                var cost = item.PriceHistory?.OrderByDescending(ph => ph.UpdateDate).FirstOrDefault()?.BaseCost ?? 0m;
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = item.Barcode;
                worksheet.Cell(currentRow, 2).Value = item.Name;
                worksheet.Cell(currentRow, 3).Value = item.SubCategory?.Category?.Name ?? "";
                worksheet.Cell(currentRow, 4).Value = item.Stock;
                worksheet.Cell(currentRow, 5).Value = cost;
                worksheet.Cell(currentRow, 6).Value = item.Stock * cost;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BajaRotacion_{days}dias_{DateTime.Now:yyyyMMdd}.xlsx");
        }
    }
}
