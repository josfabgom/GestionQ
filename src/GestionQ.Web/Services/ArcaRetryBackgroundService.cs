using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionQ.Infrastructure.Data;
using GestionQ.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GestionQ.Web.Services
{
    /// <summary>
    /// Worker en segundo plano que reintenta automáticamente:
    /// 1) Facturas POS/Web marcadas para ARCA que quedaron sin CAE por cortes de internet o caídas de ARCA.
    /// 2) Notas de Crédito pendientes para ventas facturadas que fueron anuladas.
    /// </summary>
    public class ArcaRetryBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ArcaRetryBackgroundService> _logger;

        public ArcaRetryBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<ArcaRetryBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Esperar a que inicialice la base de datos al arrancar el servicio
            try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
            catch (OperationCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingArcaTasksAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en ciclo de ArcaRetryBackgroundService");
                }

                try { await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }

        private async Task ProcessPendingArcaTasksAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var invoicing = scope.ServiceProvider.GetRequiredService<IElectronicInvoicingService>();
            var automation = scope.ServiceProvider.GetRequiredService<IArcaAutomationService>();

            // Solo operar si hay certificado ARCA configurado
            var certStatus = await invoicing.GetCertificateStatusAsync();
            if (!certStatus.IsConfigured || certStatus.IsExpired)
            {
                return;
            }

            var cutoffDate = DateTime.Now.AddDays(-5); // Límite normativo ARCA para Productos

            var pendingSaleIds = await db.Sales
                .AsNoTracking()
                .Where(s => s.RequestElectronicInvoice
                    && !s.IsCancelled
                    && s.Date >= cutoffDate
                    && (s.ElectronicInvoice == null
                        || (s.ElectronicInvoice.Status != "Approved" && s.ElectronicInvoice.Status != "Aprobado")))
                .OrderBy(s => s.Date)
                .Select(s => s.Id)
                .Take(10)
                .ToListAsync(ct);

            var cancelledInvoicedSales = await db.Sales
                .AsNoTracking()
                .Include(s => s.PointOfSale)
                .Where(s => s.IsCancelled
                    && s.Date >= cutoffDate
                    && s.ElectronicInvoice != null
                    && (s.ElectronicInvoice.Status == "Approved" || s.ElectronicInvoice.Status == "Aprobado"))
                .OrderBy(s => s.Date)
                .Take(10)
                .ToListAsync(ct);

            if (!pendingSaleIds.Any() && !cancelledInvoicedSales.Any())
            {
                return;
            }

            // Verificar que ARCA responda antes de procesar el lote
            bool arcaOnline = await invoicing.CheckInfrastructureStatusAsync();
            if (!arcaOnline)
            {
                _logger.LogDebug("ARCA no responde (FEDummy). Se reintentará en el próximo ciclo.");
                return;
            }

            foreach (var saleId in pendingSaleIds)
            {
                if (ct.IsCancellationRequested) break;
                try
                {
                    var res = await automation.EnsureInvoiceForSaleAsync(saleId);
                    if (res.Success)
                    {
                        _logger.LogInformation("Worker ARCA autorizó automáticamente venta #{SaleId}: {Voucher} (CAE {CAE})", saleId, res.VoucherNumber, res.CAE);
                    }
                    else
                    {
                        _logger.LogWarning("Worker ARCA no pudo autorizar venta #{SaleId}: {Msg}", saleId, res.Message);
                    }
                    await Task.Delay(500, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en worker ARCA al procesar venta #{SaleId}", saleId);
                }
            }

            foreach (var sale in cancelledInvoicedSales)
            {
                if (ct.IsCancellationRequested) break;
                try
                {
                    string ncMarker = $"Nota de Crédito para Venta #{sale.FormattedTicketNumber}";
                    bool hasNc = await db.ElectronicInvoices.AsNoTracking().AnyAsync(e =>
                        (e.InvoiceTypeCode == 3 || e.InvoiceTypeCode == 8 || e.InvoiceTypeCode == 13)
                        && e.ErrorMessage == ncMarker
                        && (e.Status == "Approved" || e.Status == "Aprobado"), ct);

                    if (!hasNc)
                    {
                        var ncRes = await automation.EmitCreditNoteForCancelledSaleAsync(sale.Id);
                        if (ncRes.Success)
                        {
                            _logger.LogInformation("Worker ARCA emitió Nota de Crédito automática para venta anulada #{SaleId}: {Msg}", sale.Id, ncRes.Message);
                        }
                        await Task.Delay(500, ct);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en worker ARCA al emitir NC para venta #{SaleId}", sale.Id);
                }
            }
        }
    }
}
