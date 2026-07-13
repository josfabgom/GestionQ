using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionQ.Infrastructure.Data;
using GestionQ.Web.Models;
using Microsoft.AspNetCore.Authorization;
using GestionQ.Domain.Constants;

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
                .Include(s => s.Payments)
                .ThenInclude(p => p.PaymentMethod)
                .Where(s => s.Date >= start && s.Date <= endAdjusted && !s.IsCancelled);

            var sales = await salesQuery.ToListAsync();

            viewModel.TotalSalesAmount = sales.Sum(s => s.TotalAmount);
            viewModel.TotalItemsSold = sales.SelectMany(s => s.Items).Sum(i => i.Quantity);

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

            return View(viewModel);
        }
    }
}
