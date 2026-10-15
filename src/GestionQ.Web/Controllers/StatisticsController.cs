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
                .Include(s => s.Items)
                    .ThenInclude(i => i.Presentation)
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
                    TotalLooseUnits = g.Where(i => i.PresentationId == null || i.Presentation == null || i.Presentation.Quantity <= 1).Sum(i => i.Quantity),
                    TotalBulkPackages = g.Where(i => i.PresentationId != null && i.Presentation != null && i.Presentation.Quantity > 1).Sum(i => i.Quantity / i.Presentation.Quantity),
                    TotalQuantity = g.Sum(i => i.Quantity),
                    TotalAmount = g.Sum(i => i.Quantity * i.UnitPrice - i.DiscountAmount),
                    Presentations = g.GroupBy(i => i.PresentationId).Select(pg => new PresentationSaleStat
                    {
                        PresentationName = pg.Key.HasValue && pg.First().Presentation != null ? (pg.First().Presentation.IsBulk ? $"Bulto x{pg.First().Presentation.Quantity:0.##}" : $"Unidad x{pg.First().Presentation.Quantity:0.##}") : "Suelto",
                        TotalUnits = pg.Sum(i => i.Quantity),
                        TotalPackages = pg.Key.HasValue && pg.First().Presentation != null && pg.First().Presentation.Quantity > 0 ? Math.Round(pg.Sum(i => i.Quantity) / pg.First().Presentation.Quantity, 2) : 0
                    }).ToList() // Base price minus discount
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
                .Include(s => s.Items)
                .ThenInclude(i => i.Presentation)
                .Where(s => s.Date >= start && s.Date <= endAdjusted && !s.IsCancelled);

            var sales = await salesQuery.ToListAsync();

            viewModel.SalesByProduct = sales
                .SelectMany(s => s.Items)
                .GroupBy(i => i.ProductId)
                .Select(g => new ProductSaleStat
                {
                    ProductId = g.Key,
                    ProductName = g.First().Product?.Name ?? g.First().CustomName ?? "Producto Desconocido",
                    TotalLooseUnits = g.Where(i => i.PresentationId == null || i.Presentation == null || i.Presentation.Quantity <= 1).Sum(i => i.Quantity),
                    TotalBulkPackages = g.Where(i => i.PresentationId != null && i.Presentation != null && i.Presentation.Quantity > 1).Sum(i => i.Quantity / i.Presentation.Quantity),
                    TotalQuantity = g.Sum(i => i.Quantity),
                    TotalAmount = g.Sum(i => i.Quantity * i.UnitPrice - i.DiscountAmount),
                    Presentations = g.GroupBy(i => i.PresentationId).Select(pg => new PresentationSaleStat
                    {
                        PresentationName = pg.Key.HasValue && pg.First().Presentation != null ? (pg.First().Presentation.IsBulk ? $"Bulto x{pg.First().Presentation.Quantity:0.##}" : $"Unidad x{pg.First().Presentation.Quantity:0.##}") : "Suelto",
                        TotalUnits = pg.Sum(i => i.Quantity),
                        TotalPackages = pg.Key.HasValue && pg.First().Presentation != null && pg.First().Presentation.Quantity > 0 ? Math.Round(pg.Sum(i => i.Quantity) / pg.First().Presentation.Quantity, 2) : 0
                    }).ToList()
                })
                .OrderByDescending(p => p.TotalQuantity) // de mayor a menor por cantidad (o TotalAmount?) 
                .ToList();

            return View(viewModel);
        }

        private async Task<ProfitabilityViewModel> GetProfitabilityDataAsync(DateTime start, DateTime endAdjusted)
        {
            var sales = await _context.Sales
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.SubCategory)
                            .ThenInclude(sc => sc.Category)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.PriceHistory)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.VatRate)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Presentation)
                .Where(s => s.Date >= start && s.Date <= endAdjusted && !s.IsCancelled)
                .ToListAsync();

            var flatItems = new List<(
                int ProductId,
                int? PresentationId,
                GestionQ.Domain.Entities.Product? Product,
                GestionQ.Domain.Entities.ProductPresentation? Presentation,
                string? CustomName,
                decimal Quantity,
                decimal LineCost,
                decimal LineRevenue,
                decimal LineNetSales,
                decimal LineVat,
                decimal LineInternalTax,
                decimal LineRealProfit,
                decimal VatRatePercent
            )>();

            foreach (var s in sales)
            {
                var saleItemsNet = s.Items.Sum(x => Math.Max(0, (x.Quantity * x.UnitPrice) - x.DiscountAmount));
                var saleDiscount = (saleItemsNet > 0 && s.TotalAmount < saleItemsNet && s.TotalAmount >= 0)
                    ? (saleItemsNet - s.TotalAmount)
                    : 0m;

                foreach (var item in s.Items)
                {
                    // Costo histórico a la fecha de la venta
                    var histPrice = item.Product?.PriceHistory?
                        .Where(ph => ph.UpdateDate <= s.Date)
                        .OrderByDescending(ph => ph.UpdateDate)
                        .FirstOrDefault();

                    if (histPrice == null)
                    {
                        histPrice = item.Product?.PriceHistory?.OrderBy(ph => ph.UpdateDate).FirstOrDefault()
                                  ?? item.Product?.PriceHistory?.OrderByDescending(ph => ph.UpdateDate).FirstOrDefault();
                    }

                    var unitCost = histPrice?.BaseCost ?? 0m;
                    var lineCost = item.Quantity * unitCost;
                    var internalTaxUnit = histPrice?.InternalTax ?? 0m;

                    // Descuento prorrateado de venta / medios de pago
                    var itemNet = Math.Max(0, (item.Quantity * item.UnitPrice) - item.DiscountAmount);
                    var lineRevenue = itemNet;
                    if (saleDiscount > 0 && saleItemsNet > 0)
                    {
                        var itemProratedDiscount = (itemNet / saleItemsNet) * saleDiscount;
                        lineRevenue = Math.Max(0, itemNet - itemProratedDiscount);
                    }

                    // Desglose impositivo (IVA e Impuestos Internos)
                    var vatRatePercent = item.Product?.VatRate?.Rate ?? 21.0m;
                    var totalLineInternalTax = internalTaxUnit * item.Quantity;
                    var lineInternalTax = Math.Min(totalLineInternalTax, lineRevenue);
                    var revenueSubjectToVat = Math.Max(0, lineRevenue - lineInternalTax);
                    var lineNetSales = vatRatePercent > 0 ? (revenueSubjectToVat / (1m + (vatRatePercent / 100m))) : revenueSubjectToVat;
                    var lineVat = revenueSubjectToVat - lineNetSales;
                    var lineRealProfit = lineNetSales - lineCost;

                    flatItems.Add((
                        item.ProductId,
                        item.PresentationId,
                        item.Product,
                        item.Presentation,
                        item.CustomName,
                        item.Quantity,
                        lineCost,
                        lineRevenue,
                        lineNetSales,
                        lineVat,
                        lineInternalTax,
                        lineRealProfit,
                        vatRatePercent
                    ));
                }
            }

            var items = flatItems
                .GroupBy(f => f.ProductId)
                .Select(g =>
                {
                    var product = g.First().Product;
                    var latestProductCost = product?.PriceHistory?.OrderByDescending(ph => ph.UpdateDate).FirstOrDefault()?.BaseCost ?? 0m;
                    var totalQty = g.Sum(x => x.Quantity);
                    var totalCost = g.Sum(x => x.LineCost);
                    var totalRevenue = g.Sum(x => x.LineRevenue);
                    var totalNetSales = g.Sum(x => x.LineNetSales);
                    var totalVat = g.Sum(x => x.LineVat);
                    var totalInternalTax = g.Sum(x => x.LineInternalTax);

                    var netProfit = totalRevenue - totalCost; // Ganancia de mostrador
                    var realNetProfit = totalNetSales - totalCost; // Ganancia neta real libre de impuestos

                    var avgUnitCost = totalQty > 0 ? (totalCost / totalQty) : latestProductCost;
                    var hasZeroCost = avgUnitCost <= 0;
                    var margin = totalRevenue > 0 ? (netProfit / totalRevenue) * 100m : 0m;
                    var markup = totalCost > 0 ? (netProfit / totalCost) * 100m : 0m;
                    var realMargin = totalNetSales > 0 ? (realNetProfit / totalNetSales) * 100m : 0m;
                    var realMarkup = totalCost > 0 ? (realNetProfit / totalCost) * 100m : 0m;
                    var vatRate = g.First().VatRatePercent;

                    var presentationStats = g
                        .GroupBy(f => f.PresentationId)
                        .Select(pg =>
                        {
                            var pres = pg.First().Presentation;
                            var isUnit = !pg.Key.HasValue || pres == null;
                            var presName = isUnit
                                ? "Venta Unitaria (Suelto)"
                                : (!string.IsNullOrWhiteSpace(pres!.Name) ? pres.Name : (pres.IsBulk ? $"Bulto x{pres.Quantity:0.##}" : $"Pack x{pres.Quantity:0.##}"));
                            var pkgUnits = pres != null && pres.Quantity > 0 ? pres.Quantity : 1m;
                            var pTotalUnits = pg.Sum(x => x.Quantity);
                            var pTotalPkgs = (!isUnit && pres!.Quantity > 0) ? Math.Round(pTotalUnits / pres.Quantity, 2) : 0m;
                            var pTotalCost = pg.Sum(x => x.LineCost);
                            var pTotalRev = pg.Sum(x => x.LineRevenue);
                            var pTotalNetSales = pg.Sum(x => x.LineNetSales);
                            var pTotalVat = pg.Sum(x => x.LineVat);
                            var pTotalInternalTax = pg.Sum(x => x.LineInternalTax);

                            var pNetProfit = pTotalRev - pTotalCost;
                            var pRealNetProfit = pTotalNetSales - pTotalCost;

                            var pAvgUnitCost = pTotalUnits > 0 ? (pTotalCost / pTotalUnits) : avgUnitCost;
                            var pPkgCost = pAvgUnitCost * pkgUnits;
                            var pMargin = pTotalRev > 0 ? (pNetProfit / pTotalRev) * 100m : 0m;
                            var pMarkup = pTotalCost > 0 ? (pNetProfit / pTotalCost) * 100m : 0m;
                            var pRealMargin = pTotalNetSales > 0 ? (pRealNetProfit / pTotalNetSales) * 100m : 0m;
                            var pRealMarkup = pTotalCost > 0 ? (pRealNetProfit / pTotalCost) * 100m : 0m;
                            var pHasZeroCost = pAvgUnitCost <= 0;

                            return new ProfitabilityPresentationItemViewModel
                            {
                                PresentationId = pg.Key,
                                PresentationName = presName,
                                Barcode = pres?.Barcode ?? "",
                                IsBulk = pres?.IsBulk ?? false,
                                PackageQuantity = pkgUnits,
                                TotalPackages = pTotalPkgs,
                                TotalUnits = pTotalUnits,
                                UnitCost = pAvgUnitCost,
                                PackageCost = pPkgCost,
                                TotalCost = pTotalCost,
                                TotalRevenue = pTotalRev,
                                NetSales = pTotalNetSales,
                                VatRatePercentage = vatRate,
                                VatAmount = pTotalVat,
                                InternalTaxAmount = pTotalInternalTax,
                                NetProfit = pNetProfit,
                                RealNetProfit = pRealNetProfit,
                                MarginPercentage = pMargin,
                                MarkupPercentage = pMarkup,
                                RealMarginPercentage = pRealMargin,
                                RealMarkupPercentage = pRealMarkup,
                                HasZeroCost = pHasZeroCost
                            };
                        })
                        .OrderBy(p => p.PresentationId.HasValue ? 1 : 0)
                        .ThenBy(p => p.PresentationName)
                        .ToList();

                    return new ProfitabilityItemViewModel
                    {
                        ProductId = g.Key,
                        InternalCode = product?.InternalCode ?? 0,
                        Barcode = product?.Barcode ?? "",
                        ProductName = product?.Name ?? g.First().CustomName ?? "Producto Desconocido",
                        CategoryName = product?.SubCategory?.Category?.Name ?? "Sin Categoría",
                        QuantitySold = totalQty,
                        UnitCost = avgUnitCost,
                        TotalCost = totalCost,
                        TotalRevenue = totalRevenue,
                        NetSales = totalNetSales,
                        VatRatePercentage = vatRate,
                        VatAmount = totalVat,
                        InternalTaxAmount = totalInternalTax,
                        NetProfit = netProfit,
                        RealNetProfit = realNetProfit,
                        MarginPercentage = margin,
                        MarkupPercentage = markup,
                        RealMarginPercentage = realMargin,
                        RealMarkupPercentage = realMarkup,
                        HasZeroCost = hasZeroCost,
                        Presentations = presentationStats
                    };
                })
                .OrderByDescending(p => p.RealNetProfit)
                .ToList();

            // Consultar Compras del período para cálculo de IVA Crédito Fiscal
            var purchases = await _context.Purchases
                .Include(p => p.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(pr => pr.VatRate)
                .Where(p => p.Date >= start && p.Date <= endAdjusted && p.Status != GestionQ.Domain.Entities.PurchaseStatus.Cancelled)
                .ToListAsync();

            decimal totalPurchasesNet = 0m;
            decimal totalPurchaseVatCredit = 0m;
            decimal totalPurchasesAmount = 0m;

            foreach (var p in purchases)
            {
                totalPurchasesAmount += p.TotalAmount;
                foreach (var pi in p.Items)
                {
                    var pQty = pi.ReceivedQuantity ?? pi.Quantity;
                    var pNet = pQty * pi.UnitCost;
                    var pVatRate = pi.Product?.VatRate?.Rate ?? 21.0m;
                    var pVat = pNet * (pVatRate / 100m);
                    totalPurchasesNet += pNet;
                    totalPurchaseVatCredit += pVat;
                }
            }

            var totalSalesAmount = items.Sum(i => i.TotalRevenue);
            var totalCostAmount = items.Sum(i => i.TotalCost);
            var totalProfitAmount = items.Sum(i => i.NetProfit);
            var totalNetSalesAmount = items.Sum(i => i.NetSales);
            var totalVatAmount = items.Sum(i => i.VatAmount);
            var totalInternalTaxAmount = items.Sum(i => i.InternalTaxAmount);
            var totalRealNetProfit = totalNetSalesAmount - totalCostAmount;

            // Participación sobre la Ganancia Neta Real (% Pareto / Aporte)
            foreach (var item in items)
            {
                item.SharePercentage = totalRealNetProfit > 0
                    ? (item.RealNetProfit / totalRealNetProfit) * 100m
                    : (totalProfitAmount > 0 ? (item.NetProfit / totalProfitAmount) * 100m : 0m);

                foreach (var pres in item.Presentations)
                {
                    pres.SharePercentage = totalRealNetProfit > 0
                        ? (pres.RealNetProfit / totalRealNetProfit) * 100m
                        : (totalProfitAmount > 0 ? (pres.NetProfit / totalProfitAmount) * 100m : 0m);
                }
            }

            return new ProfitabilityViewModel
            {
                StartDate = start,
                EndDate = endAdjusted,
                TotalSalesAmount = totalSalesAmount,
                TotalCostAmount = totalCostAmount,
                TotalProfitAmount = totalProfitAmount,
                ProfitMarginPercentage = totalSalesAmount > 0 ? (totalProfitAmount / totalSalesAmount) * 100m : 0m,
                ProfitMarkupPercentage = totalCostAmount > 0 ? (totalProfitAmount / totalCostAmount) * 100m : 0m,
                TotalNetSalesAmount = totalNetSalesAmount,
                TotalVatAmount = totalVatAmount,
                TotalInternalTaxAmount = totalInternalTaxAmount,
                TotalRealNetProfit = totalRealNetProfit,
                RealNetMarginPercentage = totalNetSalesAmount > 0 ? (totalRealNetProfit / totalNetSalesAmount) * 100m : 0m,
                RealNetMarkupPercentage = totalCostAmount > 0 ? (totalRealNetProfit / totalCostAmount) * 100m : 0m,
                TotalPurchasesAmount = totalPurchasesAmount,
                TotalPurchasesNet = totalPurchasesNet,
                TotalPurchaseVatCredit = totalPurchaseVatCredit,
                Items = items
            };
        }

        public async Task<IActionResult> Profitability(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today;
            var end = endDate ?? DateTime.Today;
            var endAdjusted = end.Date.AddDays(1).AddTicks(-1);

            var viewModel = await GetProfitabilityDataAsync(start, endAdjusted);
            viewModel.StartDate = start;
            viewModel.EndDate = end;

            return View(viewModel);
        }

        public async Task<IActionResult> LowStockAlert()
        {
            var products = await _context.Products
                .Include(p => p.PriceHistory)
                .Include(p => p.SubCategory)
                    .ThenInclude(sc => sc.Category)
                .Where(p => p.IsActive && p.Stock <= p.MinimumStock)
                .OrderBy(p => p.SubCategory != null && p.SubCategory.Category != null ? p.SubCategory.Category.Name : "")
                .ThenBy(p => p.Name)
                .ToListAsync();

            var viewModel = new LowStockReportViewModel
            {
                Items = products.Select(p => new LowStockReportItemViewModel
                {
                    ProductId = p.Id,
                    InternalCode = p.InternalCode,
                    Barcode = p.Barcode ?? "",
                    ProductName = p.Name,
                    CategoryName = p.SubCategory?.Category?.Name ?? "Sin Categoría",
                    Stock = p.Stock,
                    MinimumStock = p.MinimumStock,
                    UnitCost = p.PriceHistory?.OrderByDescending(ph => ph.UpdateDate).FirstOrDefault()?.BaseCost ?? 0m,
                    Price = p.Price
                }).ToList()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> StagnantStock(int days = 30)
        {
            if (days <= 0) days = 30;
            var cutoffDate = DateTime.Now.AddDays(-days);

            // Get products that haven't been sold since cutoffDate
            var activeProducts = await _context.Products
                .Include(p => p.PriceHistory)
                .Include(p => p.SubCategory)
                    .ThenInclude(sc => sc.Category)
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
                .Select(p => new StagnantStockReportItemViewModel
                {
                    ProductId = p.Id,
                    InternalCode = p.InternalCode,
                    Barcode = p.Barcode ?? "",
                    ProductName = p.Name,
                    CategoryName = p.SubCategory?.Category?.Name ?? "Sin Categoría",
                    Stock = p.Stock,
                    UnitCost = p.PriceHistory?.OrderByDescending(ph => ph.UpdateDate).FirstOrDefault()?.BaseCost ?? 0m,
                    Price = p.Price
                })
                .OrderByDescending(p => p.TiedUpCapital)
                .ToList();

            var viewModel = new StagnantStockReportViewModel
            {
                Days = days,
                Items = stagnant
            };

            return View(viewModel);
        }

        public async Task<IActionResult> ExportProfitabilityExcel(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today;
            var end = endDate ?? DateTime.Today;
            var endAdjusted = end.Date.AddDays(1).AddTicks(-1);

            var viewModel = await GetProfitabilityDataAsync(start, endAdjusted);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Rentabilidad Impositiva");
            var currentRow = 1;

            worksheet.Cell(currentRow, 1).Value = "Código";
            worksheet.Cell(currentRow, 2).Value = "Producto / Presentación";
            worksheet.Cell(currentRow, 3).Value = "Categoría";
            worksheet.Cell(currentRow, 4).Value = "Cant. Vendida";
            worksheet.Cell(currentRow, 5).Value = "Costo Unit. Neto";
            worksheet.Cell(currentRow, 6).Value = "Costo Total Neto";
            worksheet.Cell(currentRow, 7).Value = "Recaudación (PVP)";
            worksheet.Cell(currentRow, 8).Value = "Venta Neta (sin IVA)";
            worksheet.Cell(currentRow, 9).Value = "IVA Débito ($)";
            worksheet.Cell(currentRow, 10).Value = "Alícuota IVA";
            worksheet.Cell(currentRow, 11).Value = "Imp. Internos ($)";
            worksheet.Cell(currentRow, 12).Value = "Ganancia Neta Real ($)";
            worksheet.Cell(currentRow, 13).Value = "Margen Real %";
            worksheet.Cell(currentRow, 14).Value = "Markup Real %";
            worksheet.Cell(currentRow, 15).Value = "% Aporte Real";
            worksheet.Cell(currentRow, 16).Value = "Estado Costo";

            worksheet.Range(1, 1, 1, 16).Style.Font.Bold = true;
            worksheet.Range(1, 1, 1, 16).Style.Fill.BackgroundColor = XLColor.FromHtml("#1e293b");
            worksheet.Range(1, 1, 1, 16).Style.Font.FontColor = XLColor.White;

            foreach (var item in viewModel.Items)
            {
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = !string.IsNullOrEmpty(item.Barcode) ? item.Barcode : item.InternalCode.ToString();
                worksheet.Cell(currentRow, 2).Value = item.ProductName;
                worksheet.Cell(currentRow, 3).Value = item.CategoryName;
                worksheet.Cell(currentRow, 4).Value = item.QuantitySold;
                worksheet.Cell(currentRow, 5).Value = item.UnitCost;
                worksheet.Cell(currentRow, 6).Value = item.TotalCost;
                worksheet.Cell(currentRow, 7).Value = item.TotalRevenue;
                worksheet.Cell(currentRow, 8).Value = item.NetSales;
                worksheet.Cell(currentRow, 9).Value = item.VatAmount;
                worksheet.Cell(currentRow, 10).Value = $"{item.VatRatePercentage:0.##}%";
                worksheet.Cell(currentRow, 11).Value = item.InternalTaxAmount;
                worksheet.Cell(currentRow, 12).Value = item.RealNetProfit;
                worksheet.Cell(currentRow, 13).Value = $"{item.RealMarginPercentage:N1}%";
                worksheet.Cell(currentRow, 14).Value = $"{item.RealMarkupPercentage:N1}%";
                worksheet.Cell(currentRow, 15).Value = $"{item.SharePercentage:N1}%";
                worksheet.Cell(currentRow, 16).Value = item.HasZeroCost ? "⚠️ Sin Costo" : "OK";

                worksheet.Range(currentRow, 1, currentRow, 16).Style.Font.Bold = true;
                if (item.HasZeroCost)
                {
                    worksheet.Cell(currentRow, 16).Style.Font.FontColor = XLColor.Red;
                }

                if (item.HasPresentations)
                {
                    foreach (var pres in item.Presentations)
                    {
                        currentRow++;
                        worksheet.Cell(currentRow, 1).Value = pres.Barcode;
                        worksheet.Cell(currentRow, 2).Value = pres.PresentationId == null
                            ? "   ↳ " + pres.PresentationName
                            : $"   ↳ {pres.PresentationName} ({(pres.TotalPackages > 0 ? $"{pres.TotalPackages:0.##} bultos / " : "")}{pres.TotalUnits:0.##} un.)";
                        worksheet.Cell(currentRow, 3).Value = pres.PresentationId == null ? "Unidad" : "Bulto";
                        worksheet.Cell(currentRow, 4).Value = pres.TotalUnits;
                        worksheet.Cell(currentRow, 5).Value = pres.UnitCost;
                        worksheet.Cell(currentRow, 6).Value = pres.TotalCost;
                        worksheet.Cell(currentRow, 7).Value = pres.TotalRevenue;
                        worksheet.Cell(currentRow, 8).Value = pres.NetSales;
                        worksheet.Cell(currentRow, 9).Value = pres.VatAmount;
                        worksheet.Cell(currentRow, 10).Value = $"{pres.VatRatePercentage:0.##}%";
                        worksheet.Cell(currentRow, 11).Value = pres.InternalTaxAmount;
                        worksheet.Cell(currentRow, 12).Value = pres.RealNetProfit;
                        worksheet.Cell(currentRow, 13).Value = $"{pres.RealMarginPercentage:N1}%";
                        worksheet.Cell(currentRow, 14).Value = $"{pres.RealMarkupPercentage:N1}%";
                        worksheet.Cell(currentRow, 15).Value = $"{pres.SharePercentage:N1}%";
                        worksheet.Cell(currentRow, 16).Value = pres.HasZeroCost ? "⚠️ Sin Costo" : "OK";

                        worksheet.Range(currentRow, 1, currentRow, 16).Style.Font.Italic = true;
                        worksheet.Range(currentRow, 1, currentRow, 16).Style.Font.FontColor = XLColor.DarkSlateGray;
                        if (pres.HasZeroCost)
                        {
                            worksheet.Cell(currentRow, 16).Style.Font.FontColor = XLColor.Red;
                        }
                    }
                }
            }

            // Fila de totales
            if (viewModel.Items.Any())
            {
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "";
                worksheet.Cell(currentRow, 2).Value = "TOTALES GENERALES:";
                worksheet.Cell(currentRow, 3).Value = "";
                worksheet.Cell(currentRow, 4).Value = viewModel.Items.Sum(i => i.QuantitySold);
                worksheet.Cell(currentRow, 5).Value = "";
                worksheet.Cell(currentRow, 6).Value = viewModel.TotalCostAmount;
                worksheet.Cell(currentRow, 7).Value = viewModel.TotalSalesAmount;
                worksheet.Cell(currentRow, 8).Value = viewModel.TotalNetSalesAmount;
                worksheet.Cell(currentRow, 9).Value = viewModel.TotalVatAmount;
                worksheet.Cell(currentRow, 10).Value = "";
                worksheet.Cell(currentRow, 11).Value = viewModel.TotalInternalTaxAmount;
                worksheet.Cell(currentRow, 12).Value = viewModel.TotalRealNetProfit;
                worksheet.Cell(currentRow, 13).Value = $"{viewModel.RealNetMarginPercentage:N1}%";
                worksheet.Cell(currentRow, 14).Value = $"{viewModel.RealNetMarkupPercentage:N1}%";
                worksheet.Cell(currentRow, 15).Value = "100.0%";
                worksheet.Cell(currentRow, 16).Value = "";

                worksheet.Range(currentRow, 1, currentRow, 16).Style.Font.Bold = true;
                worksheet.Range(currentRow, 1, currentRow, 16).Style.Fill.BackgroundColor = XLColor.FromHtml("#f1f5f9");
            }

            // Resumen impositivo adicional
            currentRow += 2;
            worksheet.Cell(currentRow, 2).Value = "RESUMEN IMPOSITIVO DEL PERÍODO (AFIP/ARCA)";
            worksheet.Range(currentRow, 2, currentRow, 5).Merge().Style.Font.Bold = true;
            worksheet.Range(currentRow, 2, currentRow, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#334155");
            worksheet.Range(currentRow, 2, currentRow, 5).Style.Font.FontColor = XLColor.White;

            currentRow++;
            worksheet.Cell(currentRow, 2).Value = "Total Facturado / Cobrado Mostrador (PVP):";
            worksheet.Cell(currentRow, 3).Value = viewModel.TotalSalesAmount;

            currentRow++;
            worksheet.Cell(currentRow, 2).Value = "Total Ventas Netas Gravadas (Base Imponible):";
            worksheet.Cell(currentRow, 3).Value = viewModel.TotalNetSalesAmount;

            currentRow++;
            worksheet.Cell(currentRow, 2).Value = "Total IVA Débito Fiscal (Cobrado en Ventas):";
            worksheet.Cell(currentRow, 3).Value = viewModel.TotalVatAmount;

            currentRow++;
            worksheet.Cell(currentRow, 2).Value = "Total Impuestos Internos Cobrados:";
            worksheet.Cell(currentRow, 3).Value = viewModel.TotalInternalTaxAmount;

            currentRow++;
            worksheet.Cell(currentRow, 2).Value = "Total Compras Netas (Facturas Proveedor Recibidas):";
            worksheet.Cell(currentRow, 3).Value = viewModel.TotalPurchasesNet;

            currentRow++;
            worksheet.Cell(currentRow, 2).Value = "Total IVA Crédito Fiscal (Pagado en Compras):";
            worksheet.Cell(currentRow, 3).Value = viewModel.TotalPurchaseVatCredit;

            currentRow++;
            worksheet.Cell(currentRow, 2).Value = "Posición Técnica IVA Estimada (Débito - Crédito):";
            worksheet.Cell(currentRow, 3).Value = viewModel.EstimatedVatBalance;
            worksheet.Cell(currentRow, 4).Value = viewModel.EstimatedVatBalance >= 0 ? "A Pagar Estimado" : "Saldo a Favor Estimado";
            worksheet.Range(currentRow, 2, currentRow, 4).Style.Font.Bold = true;
            worksheet.Range(currentRow, 2, currentRow, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#e2e8f0");

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Rentabilidad_Impositiva_{start:yyyyMMdd}_{end:yyyyMMdd}.xlsx");
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

