using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

using GestionQ.Infrastructure.Data;
using GestionQ.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace GestionQ.Web
{
    public static class HistoricCentralCashFix
    {
        public static async Task Run(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var closedRegisters = await _context.CashRegisters
                .Include(c => c.Movements)
                .Include(c => c.Sales).ThenInclude(s => s.Payments).ThenInclude(p => p.PaymentMethod)
                .Where(c => c.ClosingDate != null)
                .ToListAsync();

            int addedCount = 0;
            foreach (var register in closedRegisters)
            {
                var alreadySynced = await _context.CentralCashMovements.AnyAsync(m => m.SourceCashRegisterId == register.Id);
                if (!alreadySynced)
                {
                    decimal netoEfectivo = (register.FinalCashBalance ?? 0m) - register.InitialBalance;
                    if (netoEfectivo > 0)
                    {
                        _context.CentralCashMovements.Add(new CentralCashMovement
                        {
                            Date = register.ClosingDate ?? DateTime.Now,
                            Type = "Ingreso",
                            Amount = netoEfectivo,
                            Concept = $"Rendición Caja POS #{register.Id} - Efectivo",
                            UserId = register.UserId,
                            SourceCashRegisterId = register.Id
                        });
                        addedCount++;
                    }
                    else if (netoEfectivo < 0)
                    {
                        _context.CentralCashMovements.Add(new CentralCashMovement
                        {
                            Date = register.ClosingDate ?? DateTime.Now,
                            Type = "Egreso",
                            Amount = Math.Abs(netoEfectivo),
                            Concept = $"Faltante/Retiro Caja POS #{register.Id} - Efectivo",
                            UserId = register.UserId,
                            SourceCashRegisterId = register.Id
                        });
                        addedCount++;
                    }

                    var otrosPagos = register.Sales.Where(s => !s.IsCancelled)
                        .SelectMany(s => s.Payments)
                        .Where(p => p.PaymentMethod != null && p.PaymentMethod.Name != "Efectivo")
                        .GroupBy(p => p.PaymentMethod!.Name)
                        .Select(g => new { Metodo = g.Key, Total = g.Sum(x => x.Amount) })
                        .Where(g => g.Total > 0)
                        .ToList();

                    foreach (var pago in otrosPagos)
                    {
                        _context.CentralCashMovements.Add(new CentralCashMovement
                        {
                            Date = register.ClosingDate ?? DateTime.Now,
                            Type = "Ingreso",
                            Amount = pago.Total,
                            Concept = $"Rendición Caja POS #{register.Id} - {pago.Metodo}",
                            UserId = register.UserId,
                            SourceCashRegisterId = register.Id
                        });
                        addedCount++;
                    }
                }
            }

            if (addedCount > 0)
            {
                await _context.SaveChangesAsync();
                Console.WriteLine($"Se agregaron {addedCount} movimientos a la Caja Central por cajas pasadas.");
            }
            else
            {
                Console.WriteLine("No había cajas pendientes de sincronizar con la Caja Central.");
            }
        }
    }
}
