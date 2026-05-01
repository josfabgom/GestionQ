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
        var totalSales = await context.Sales.SumAsync(s => s.TotalAmount);
        var totalClients = await context.Customers.CountAsync();
        var totalStockValue = await context.Products.SumAsync(p => p.Price * p.Stock);

        ViewBag.TotalSales = totalSales;
        ViewBag.TotalClients = totalClients;
        ViewBag.TotalStockValue = totalStockValue;

        return View();
    }
}
