using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionQ.Infrastructure.Data;
using GestionQ.Domain.Entities;
using GestionQ.Web.Models;

namespace GestionQ.Web.Controllers;

[Authorize]
public class HomeController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var totalSales = await context.Sales.SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;
        var totalClients = await context.Customers.CountAsync();
        var totalStockValue = await context.Products.SumAsync(p => (decimal?)(p.Price * p.Stock)) ?? 0m;

        ViewBag.TotalSales = totalSales;
        ViewBag.TotalClients = totalClients;
        ViewBag.TotalStockValue = totalStockValue;

        return View();
    }
}
