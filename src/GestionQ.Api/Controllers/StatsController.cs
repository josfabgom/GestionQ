using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using GestionQ.Infrastructure.Data;
using System.Collections.Generic;

namespace GestionQ.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [GestionQ.Api.Attributes.ApiKeyAuth]
    public class StatsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StatsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            var todaySales = await _context.Sales
                .Where(s => s.Date >= today)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

            var weeklySales = await _context.Sales
                .Where(s => s.Date >= startOfWeek)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

            var monthlySales = await _context.Sales
                .Where(s => s.Date >= startOfMonth)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

            var totalProducts = await _context.Products.CountAsync(p => p.IsActive);
            var lowStockProducts = await _context.Products.CountAsync(p => p.IsActive && p.Stock <= 5); // Assuming 5 is a threshold

            return Ok(new
            {
                todaySales,
                weeklySales,
                monthlySales,
                totalProducts,
                lowStockProducts
            });
        }

        [HttpGet("top-products")]
        public async Task<IActionResult> GetTopProducts([FromQuery] int limit = 5)
        {
            var topProducts = await _context.Sales
                .SelectMany(s => s.Items)
                .GroupBy(i => i.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalQuantity = g.Sum(i => i.Quantity),
                    TotalRevenue = g.Sum(i => i.Quantity * i.UnitPrice)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(limit)
                .Join(_context.Products, 
                      tp => tp.ProductId, 
                      p => p.Id, 
                      (tp, p) => new 
                      { 
                          p.Name, 
                          p.InternalCode, 
                          tp.TotalQuantity, 
                          tp.TotalRevenue 
                      })
                .ToListAsync();

            return Ok(topProducts);
        }

        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStock([FromQuery] int limit = 10)
        {
            // Assuming we want products with stock <= 5
            var lowStock = await _context.Products
                .Where(p => p.IsActive && p.Stock <= 5)
                .OrderBy(p => p.Stock)
                .Take(limit)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.InternalCode,
                    p.Stock
                })
                .ToListAsync();

            return Ok(lowStock);
        }

        [HttpGet("revenue-chart")]
        public async Task<IActionResult> GetRevenueChart([FromQuery] int days = 30)
        {
            var startDate = DateTime.Today.AddDays(-days);

            var revenueData = await _context.Sales
                .Where(s => s.Date >= startDate)
                .GroupBy(s => s.Date.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(s => s.TotalAmount)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            // Fill missing days with 0
            var result = new List<object>();
            for (int i = 0; i <= days; i++)
            {
                var currentDate = startDate.AddDays(i);
                var data = revenueData.FirstOrDefault(r => r.Date == currentDate);
                result.Add(new
                {
                    Date = currentDate.ToString("yyyy-MM-dd"),
                    Revenue = data?.Revenue ?? 0
                });
            }

            return Ok(result);
        }

        [HttpGet("pos-status")]
        public async Task<IActionResult> GetPosStatus()
        {
            var posList = await _context.PointsOfSale
                .Where(p => p.IsActive)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.MachineName,
                    p.LastSyncDate,
                    // Get the most recent cash register for this POS
                    CurrentRegister = _context.CashRegisters
                        .Where(cr => cr.PointOfSaleId == p.Id)
                        .OrderByDescending(cr => cr.OpeningDate)
                        .Select(cr => new
                        {
                            cr.Id,
                            cr.OpeningDate,
                            cr.ClosingDate,
                            cr.InitialBalance,
                            IsOpen = cr.ClosingDate == null,
                            Cashier = cr.User.UserName, // Assuming User has UserName or Email
                            SalesInSession = _context.Sales.Where(s => s.CashRegisterId == cr.Id && !s.IsCancelled).Sum(s => (decimal?)s.TotalAmount) ?? 0,
                            SalesCount = _context.Sales.Where(s => s.CashRegisterId == cr.Id && !s.IsCancelled).Count(), CancelledSalesAmount = _context.Sales.Where(s => s.CashRegisterId == cr.Id && s.IsCancelled).Sum(s => (decimal?)s.TotalAmount) ?? 0, CancelledSalesCount = _context.Sales.Where(s => s.CashRegisterId == cr.Id && s.IsCancelled).Count(),
                            CashMovements = _context.CashRegisterMovements
                                .Where(m => m.CashRegisterId == cr.Id)
                                .Sum(m => (decimal?)m.Amount) ?? 0,
                            RecentSales = _context.Sales
                                .Where(s => s.CashRegisterId == cr.Id)
                                .OrderByDescending(s => s.Date)
                                .Take(15)
                                .Select(s => new {
                                    s.Id,
                                    s.Date,
                                    s.TotalAmount
                                })
                                .ToList(),
                            PaymentBreakdown = _context.Sales
                                .Where(s => s.CashRegisterId == cr.Id)
                                .SelectMany(s => s.Payments)
                                .GroupBy(p => p.PaymentMethod.Name)
                                .Select(g => new {
                                    Method = g.Key,
                                    Amount = g.Sum(x => x.Amount)
                                })
                                .ToList()
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            var result = posList.Select(p => new
            {
                id = p.Id,
                name = p.Name,
                machineName = p.MachineName,
                lastSyncDate = p.LastSyncDate,
                isOpen = p.CurrentRegister?.IsOpen ?? false,
                cashier = p.CurrentRegister?.Cashier,
                openingDate = p.CurrentRegister?.OpeningDate,
                salesAmount = p.CurrentRegister?.SalesInSession ?? 0,
                salesCount = p.CurrentRegister?.SalesCount ?? 0, cancelledSalesAmount = p.CurrentRegister?.CancelledSalesAmount ?? 0, cancelledSalesCount = p.CurrentRegister?.CancelledSalesCount ?? 0,
                initialBalance = p.CurrentRegister?.InitialBalance ?? 0,
                cashBalance = (p.CurrentRegister?.InitialBalance ?? 0) 
                              + (p.CurrentRegister?.SalesInSession ?? 0) // Basic simplification of cash
                              + (p.CurrentRegister?.CashMovements ?? 0),
                recentSales = p.CurrentRegister?.RecentSales,
                paymentBreakdown = p.CurrentRegister?.PaymentBreakdown
            });

            return Ok(result);
        }

        [HttpGet("pos-history")]
        public async Task<IActionResult> GetPosHistory([FromQuery] int days = 30)
        {
            var startDate = DateTime.Today.AddDays(-days);

            var history = await _context.CashRegisters
                .Include(cr => cr.PointOfSale)
                .Include(cr => cr.User)
                .Where(cr => cr.ClosingDate != null && cr.OpeningDate >= startDate)
                .OrderByDescending(cr => cr.OpeningDate)
                .Select(cr => new
                {
                    id = cr.Id,
                    posName = cr.PointOfSale.Name,
                    cashier = cr.User.UserName,
                    openingDate = cr.OpeningDate,
                    closingDate = cr.ClosingDate,
                    initialBalance = cr.InitialBalance,
                    salesAmount = _context.Sales.Where(s => s.CashRegisterId == cr.Id).Sum(s => (decimal?)s.TotalAmount) ?? 0,
                    SalesCount = _context.Sales.Where(s => s.CashRegisterId == cr.Id && !s.IsCancelled).Count(), CancelledSalesAmount = _context.Sales.Where(s => s.CashRegisterId == cr.Id && s.IsCancelled).Sum(s => (decimal?)s.TotalAmount) ?? 0, CancelledSalesCount = _context.Sales.Where(s => s.CashRegisterId == cr.Id && s.IsCancelled).Count(),
                    paymentBreakdown = _context.Sales
                                .Where(s => s.CashRegisterId == cr.Id)
                                .SelectMany(s => s.Payments)
                                .GroupBy(p => p.PaymentMethod.Name)
                                .Select(g => new {
                                    Method = g.Key,
                                    Amount = g.Sum(x => x.Amount)
                                })
                                .ToList()
                })
                .ToListAsync();

            return Ok(history);
        }
    }
}



