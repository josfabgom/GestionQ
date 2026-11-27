using System;
using System.Linq;
using System.Threading.Tasks;
using GestionQ.Domain.Entities;
using GestionQ.Infrastructure.Data;
using GestionQ.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GestionQ.Web.Services
{
    public class ArcaAutoInvoiceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string CAE { get; set; } = string.Empty;
        public string CAEExpiration { get; set; } = string.Empty;
        public string VoucherNumber { get; set; } = string.Empty;
        public string InvoiceType { get; set; } = string.Empty;
        public string QrUrl { get; set; } = string.Empty;
        public int? InvoiceId { get; set; }
    }

    public interface IArcaAutomationService
    {
        Task<ArcaAutoInvoiceResult> EnsureInvoiceForSaleAsync(int saleId);
        Task<(bool Success, string Message)> EmitCreditNoteForCancelledSaleAsync(int saleId);
        void QueueAutoEmailIfEnabled(int invoiceId, string? customerEmail);
    }

    public class ArcaAutomationService : IArcaAutomationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IElectronicInvoicingService _invoicingService;
        private readonly IConfiguration _config;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ArcaAutomationService> _logger;

        public ArcaAutomationService(
            ApplicationDbContext context,
            IElectronicInvoicingService invoicingService,
            IConfiguration config,
            IServiceScopeFactory scopeFactory,
            ILogger<ArcaAutomationService> logger)
        {
            _context = context;
            _invoicingService = invoicingService;
            _config = config;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public static string GetInvoiceTypeDesc(int code)
        {
            return code switch
            {
                1 => "Factura A",
                3 => "Nota de Crédito A",
                6 => "Factura B",
                8 => "Nota de Crédito B",
                11 => "Factura C",
                13 => "Nota de Crédito C",
                _ => "Comprobante"
            };
        }

        public static bool IsApprovedStatus(string? status)
        {
            return string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Aprobado", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<ArcaAutoInvoiceResult> EnsureInvoiceForSaleAsync(int saleId)
        {
            var sale = await _context.Sales
                .Include(s => s.ElectronicInvoice)
                .Include(s => s.Customer)
                    .ThenInclude(c => c!.TaxCondition)
                .Include(s => s.PointOfSale)
                .Include(s => s.Items)
                    .ThenInclude(si => si.Product)
                        .ThenInclude(p => p!.VatRate)
                .FirstOrDefaultAsync(s => s.Id == saleId);

            if (sale == null)
            {
                return new ArcaAutoInvoiceResult { Success = false, Message = "La venta especificada no existe." };
            }

            if (sale.ElectronicInvoice != null && IsApprovedStatus(sale.ElectronicInvoice.Status))
            {
                var existingType = string.IsNullOrWhiteSpace(sale.ElectronicInvoice.InvoiceTypeDesc)
                    ? GetInvoiceTypeDesc(sale.ElectronicInvoice.InvoiceTypeCode)
                    : sale.ElectronicInvoice.InvoiceTypeDesc;
                var qrUrl = await _invoicingService.BuildQrUrlAsync(sale.ElectronicInvoice);

                return new ArcaAutoInvoiceResult
                {
                    Success = true,
                    Message = "Esta venta ya posee una factura electrónica autorizada.",
                    CAE = sale.ElectronicInvoice.CAE,
                    CAEExpiration = sale.ElectronicInvoice.CAEExpirationDate.ToString("dd/MM/yyyy"),
                    VoucherNumber = sale.ElectronicInvoice.FormattedVoucherNumber,
                    InvoiceType = existingType,
                    QrUrl = qrUrl,
                    InvoiceId = sale.ElectronicInvoice.Id
                };
            }

            decimal netAmount = 0;
            decimal vatAmount = 0;
            decimal exemptAmount = 0;

            foreach (var item in sale.Items)
            {
                decimal itemTotal = (item.Quantity * item.UnitPrice) - item.DiscountAmount;
                if (itemTotal < 0) itemTotal = 0;
                decimal vatRatePercent = item.Product?.VatRate?.Rate ?? 21.0m;

                if (vatRatePercent == 0)
                {
                    exemptAmount += itemTotal;
                }
                else
                {
                    decimal net = itemTotal / (1 + (vatRatePercent / 100));
                    decimal vat = itemTotal - net;
                    netAmount += net;
                    vatAmount += vat;
                }
            }

            // Ajustar si hubo descuento general/pago para que la suma coincida con TotalAmount
            decimal calculatedTotal = Math.Round(netAmount + vatAmount + exemptAmount, 2);
            decimal targetTotal = Math.Round(sale.TotalAmount, 2);
            if (calculatedTotal > 0 && Math.Abs(calculatedTotal - targetTotal) > 0.01m)
            {
                decimal ratio = targetTotal / calculatedTotal;
                netAmount = Math.Round(netAmount * ratio, 2);
                exemptAmount = Math.Round(exemptAmount * ratio, 2);
                vatAmount = Math.Round(targetTotal - netAmount - exemptAmount, 2);
            }
            else
            {
                netAmount = Math.Round(netAmount, 2);
                vatAmount = Math.Round(vatAmount, 2);
                exemptAmount = Math.Round(exemptAmount, 2);
            }

            int defaultInvoiceTypeCode = 6; // Factura B
            int defaultCondicionIvaReceptor = 5; // Consumidor Final
            string customerCuit = sale.Customer?.Cuit ?? string.Empty;
            string customerDni = sale.Customer?.Dni ?? string.Empty;
            int docTypeCode = 99;

            if (!string.IsNullOrWhiteSpace(customerCuit))
            {
                docTypeCode = 80;
            }
            else if (!string.IsNullOrWhiteSpace(customerDni))
            {
                docTypeCode = 96;
            }

            var companyTaxCondition = _config["CompanyInfo:TaxCondition"]?.ToLower() ?? "";
            bool isMonotributista = companyTaxCondition.Contains("monotributo") || companyTaxCondition.Contains("monotributista");

            if (isMonotributista)
            {
                defaultInvoiceTypeCode = 11; // Factura C
                defaultCondicionIvaReceptor = 5;
                if (sale.Customer?.TaxCondition != null)
                {
                    var taxConditionName = sale.Customer.TaxCondition.Name.ToLower();
                    if (taxConditionName.Contains("inscripto")) defaultCondicionIvaReceptor = 1;
                    else if (taxConditionName.Contains("monotributo") || taxConditionName.Contains("monotributista")) defaultCondicionIvaReceptor = 6;
                    else if (taxConditionName.Contains("exento")) defaultCondicionIvaReceptor = 4;
                }
            }
            else
            {
                if (sale.Customer?.TaxCondition != null)
                {
                    var taxConditionName = sale.Customer.TaxCondition.Name.ToLower();
                    if (taxConditionName.Contains("inscripto"))
                    {
                        defaultInvoiceTypeCode = 1;
                        defaultCondicionIvaReceptor = 1;
                    }
                    else if (taxConditionName.Contains("monotributo") || taxConditionName.Contains("monotributista"))
                    {
                        defaultInvoiceTypeCode = 6;
                        defaultCondicionIvaReceptor = 6;
                    }
                    else if (taxConditionName.Contains("exento"))
                    {
                        defaultInvoiceTypeCode = 6;
                        defaultCondicionIvaReceptor = 4;
                    }
                }
            }

            var posId = sale.PointOfSaleId ?? 0;
            var posNumber = sale.PointOfSale?.PosNumber ?? 1;

            if (posId == 0)
            {
                var defaultPos = await _context.PointsOfSale.FirstOrDefaultAsync();
                if (defaultPos != null)
                {
                    posId = defaultPos.Id;
                    posNumber = defaultPos.PosNumber;
                }
            }

            var request = new ElectronicInvoiceRequest
            {
                PointOfSaleId = posId,
                PointOfSaleNumber = posNumber,
                InvoiceTypeCode = defaultInvoiceTypeCode,
                ConceptCode = 1,
                DocTypeCode = docTypeCode,
                DocNumber = docTypeCode == 80 ? customerCuit : (docTypeCode == 96 ? customerDni : string.Empty),
                CustomerName = sale.Customer?.Name ?? "Consumidor Final",
                CustomerTaxCondition = sale.Customer?.TaxCondition?.Name ?? "Consumidor Final",
                NetAmount = netAmount,
                VatAmount = vatAmount,
                ExemptAmount = exemptAmount,
                TotalAmount = targetTotal,
                CanMisMonExt = false,
                CondicionIVAReceptorId = defaultCondicionIvaReceptor
            };

            var serviceResponse = await _invoicingService.RequestCAEAsync(request);

            if (serviceResponse.Success)
            {
                var invoice = sale.ElectronicInvoice ?? new ElectronicInvoice { SaleId = sale.Id };
                invoice.PointOfSaleId = request.PointOfSaleId;
                invoice.PointOfSaleNumber = request.PointOfSaleNumber;
                invoice.InvoiceTypeCode = request.InvoiceTypeCode;
                invoice.InvoiceTypeDesc = GetInvoiceTypeDesc(request.InvoiceTypeCode);
                invoice.InvoiceNumber = serviceResponse.InvoiceNumber;
                invoice.IssueDate = DateTime.Now;
                invoice.ConceptCode = request.ConceptCode;
                invoice.DocTypeCode = request.DocTypeCode;
                invoice.DocNumber = request.DocNumber;
                invoice.CustomerName = request.CustomerName;
                invoice.CustomerTaxCondition = request.CustomerTaxCondition;
                invoice.NetAmount = request.NetAmount;
                invoice.VatAmount = request.VatAmount;
                invoice.ExemptAmount = request.ExemptAmount;
                invoice.TotalAmount = request.TotalAmount;
                invoice.CAE = serviceResponse.CAE;
                invoice.CAEExpirationDate = serviceResponse.CAEExpirationDate;
                invoice.Status = "Approved";
                invoice.ErrorMessage = null;
                invoice.CanMisMonExt = false;
                invoice.CondicionIVAReceptorId = request.CondicionIVAReceptorId;

                if (sale.ElectronicInvoice == null)
                {
                    _context.ElectronicInvoices.Add(invoice);
                    sale.ElectronicInvoice = invoice;
                }

                var hasPrintJob = await _context.FiscalPrintJobs.AnyAsync(j => j.SaleId == sale.Id);
                if (!hasPrintJob)
                {
                    _context.FiscalPrintJobs.Add(new FiscalPrintJob
                    {
                        SaleId = sale.Id,
                        CreatedDate = DateTime.Now,
                        Status = "Pending",
                        PrinterName = "Impresora Fiscal Térmica"
                    });
                }

                await _context.SaveChangesAsync();

                QueueAutoEmailIfEnabled(invoice.Id, sale.Customer?.Email);

                var qrUrl = await _invoicingService.BuildQrUrlAsync(invoice);
                return new ArcaAutoInvoiceResult
                {
                    Success = true,
                    Message = $"Factura {invoice.InvoiceTypeDesc} N° {invoice.FormattedVoucherNumber} autorizada con éxito.",
                    CAE = invoice.CAE,
                    CAEExpiration = invoice.CAEExpirationDate.ToString("dd/MM/yyyy"),
                    VoucherNumber = invoice.FormattedVoucherNumber,
                    InvoiceType = invoice.InvoiceTypeDesc,
                    QrUrl = qrUrl,
                    InvoiceId = invoice.Id
                };
            }

            var errors = string.Join("; ", serviceResponse.Errors);
            if (errors.Contains("ORA-01034") || errors.Contains("ORA-27101") || errors.Contains("InternalServerError") || errors.Contains("/wsfev1"))
            {
                return new ArcaAutoInvoiceResult
                {
                    Success = false,
                    Message = "No se puede conectar con ARCA. Sus servidores están experimentando problemas técnicos. Por favor, intente facturar más tarde."
                };
            }

            return new ArcaAutoInvoiceResult
            {
                Success = false,
                Message = "ARCA rechazó el comprobante: " + errors
            };
        }

        public async Task<(bool Success, string Message)> EmitCreditNoteForCancelledSaleAsync(int saleId)
        {
            var sale = await _context.Sales
                .Include(s => s.ElectronicInvoice)
                .Include(s => s.PointOfSale)
                .FirstOrDefaultAsync(s => s.Id == saleId);

            if (sale == null || sale.ElectronicInvoice == null || !IsApprovedStatus(sale.ElectronicInvoice.Status))
            {
                return (true, "La venta no posee factura electrónica aprobada.");
            }

            string ncMarker = $"Nota de Crédito para Venta #{sale.FormattedTicketNumber}";
            bool alreadyHasNc = await _context.ElectronicInvoices.AnyAsync(e =>
                (e.InvoiceTypeCode == 3 || e.InvoiceTypeCode == 8 || e.InvoiceTypeCode == 13)
                && e.ErrorMessage == ncMarker
                && (e.Status == "Approved" || e.Status == "Aprobado"));

            if (alreadyHasNc)
            {
                return (true, "La Nota de Crédito ya fue emitida previamente.");
            }

            int ncDocTypeCode = sale.ElectronicInvoice.InvoiceTypeCode switch
            {
                1 => 3,  // Factura A -> Nota de Crédito A
                6 => 8,  // Factura B -> Nota de Crédito B
                11 => 13, // Factura C -> Nota de Crédito C
                _ => 0
            };

            if (ncDocTypeCode == 0)
            {
                return (false, "Tipo de comprobante original no soporta Nota de Crédito automática.");
            }

            int posId = sale.PointOfSaleId ?? sale.ElectronicInvoice.PointOfSaleId;
            int posNumber = sale.PointOfSale?.PosNumber ?? sale.ElectronicInvoice.PointOfSaleNumber;

            var invoiceRequest = new ElectronicInvoiceRequest
            {
                PointOfSaleId = posId,
                PointOfSaleNumber = posNumber,
                InvoiceTypeCode = ncDocTypeCode,
                ConceptCode = sale.ElectronicInvoice.ConceptCode > 0 ? sale.ElectronicInvoice.ConceptCode : 1,
                DocTypeCode = sale.ElectronicInvoice.DocTypeCode,
                DocNumber = sale.ElectronicInvoice.DocNumber ?? "0",
                CustomerName = sale.ElectronicInvoice.CustomerName,
                CustomerTaxCondition = sale.ElectronicInvoice.CustomerTaxCondition,
                TotalAmount = sale.ElectronicInvoice.TotalAmount,
                NetAmount = sale.ElectronicInvoice.NetAmount,
                VatAmount = sale.ElectronicInvoice.VatAmount,
                ExemptAmount = sale.ElectronicInvoice.ExemptAmount,
                CondicionIVAReceptorId = sale.ElectronicInvoice.CondicionIVAReceptorId > 0 ? sale.ElectronicInvoice.CondicionIVAReceptorId : 5,
                CanMisMonExt = sale.ElectronicInvoice.CanMisMonExt
            };

            try
            {
                var afipResponse = await _invoicingService.RequestCAEAsync(invoiceRequest);
                var ncInvoice = new ElectronicInvoice
                {
                    SaleId = null, // Evitar colisión con índice único 1-a-1 de la factura original
                    PointOfSaleId = posId,
                    PointOfSaleNumber = posNumber,
                    InvoiceTypeCode = ncDocTypeCode,
                    InvoiceTypeDesc = GetInvoiceTypeDesc(ncDocTypeCode),
                    InvoiceNumber = afipResponse.Success ? afipResponse.InvoiceNumber : 0,
                    ConceptCode = invoiceRequest.ConceptCode,
                    DocTypeCode = invoiceRequest.DocTypeCode,
                    DocNumber = invoiceRequest.DocNumber,
                    CustomerName = sale.ElectronicInvoice.CustomerName,
                    CustomerTaxCondition = sale.ElectronicInvoice.CustomerTaxCondition,
                    TotalAmount = invoiceRequest.TotalAmount,
                    NetAmount = invoiceRequest.NetAmount,
                    VatAmount = invoiceRequest.VatAmount,
                    ExemptAmount = invoiceRequest.ExemptAmount,
                    IssueDate = DateTime.Now,
                    Status = afipResponse.Success ? "Approved" : "Rejected",
                    CAE = afipResponse.Success ? afipResponse.CAE : string.Empty,
                    CAEExpirationDate = afipResponse.Success ? afipResponse.CAEExpirationDate : DateTime.Now,
                    ErrorMessage = afipResponse.Success
                        ? ncMarker
                        : string.Join("; ", afipResponse.Errors),
                    CondicionIVAReceptorId = invoiceRequest.CondicionIVAReceptorId
                };

                _context.ElectronicInvoices.Add(ncInvoice);
                await _context.SaveChangesAsync();

                return afipResponse.Success
                    ? (true, $"Nota de Crédito {ncInvoice.FormattedVoucherNumber} emitida correctamente.")
                    : (false, ncInvoice.ErrorMessage ?? "Error al emitir Nota de Crédito en ARCA.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al emitir Nota de Crédito automática para venta {SaleId}", saleId);
                return (false, ex.Message);
            }
        }

        public void QueueAutoEmailIfEnabled(int invoiceId, string? customerEmail)
        {
            if (string.IsNullOrWhiteSpace(customerEmail)) return;

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var autoSendSetting = await db.SystemSettings.AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Key == "Afip_AutoSendEmail");

                    if (!string.Equals(autoSendSetting?.Value, "true", StringComparison.OrdinalIgnoreCase))
                        return;

                    var emailService = scope.ServiceProvider.GetService<IEmailService>();
                    var pdfGenerator = scope.ServiceProvider.GetService<IInvoicePdfGenerator>();
                    var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                    if (emailService == null || pdfGenerator == null) return;

                    var invoice = await db.ElectronicInvoices
                        .Include(e => e.PointOfSale)
                        .Include(e => e.Sale)
                            .ThenInclude(s => s!.Items)
                                .ThenInclude(si => si.Product)
                        .FirstOrDefaultAsync(e => e.Id == invoiceId);

                    if (invoice == null || !IsApprovedStatus(invoice.Status)) return;

                    string companyName = cfg["CompanyInfo:Name"] ?? "GestionQ";
                    string companyCuit = (await db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == "Afip_Cuit"))?.Value
                        ?? cfg["CompanyInfo:Cuit"] ?? "";
                    string companyCondition = cfg["CompanyInfo:TaxCondition"] ?? "";
                    string companyAddress = cfg["CompanyInfo:Address"] ?? "";

                    byte[] pdfBytes = pdfGenerator.GeneratePdf(invoice, companyName, companyCuit, companyCondition, companyAddress);
                    string subject = $"Factura {invoice.InvoiceTypeDesc} N° {invoice.FormattedVoucherNumber} de {companyName}";
                    string htmlMessage = $"<h3>Hola,</h3><p>Adjunto a este correo encontrarás tu comprobante electrónico (<strong>{invoice.InvoiceTypeDesc} N° {invoice.FormattedVoucherNumber}</strong>).</p><p>CAE: <strong>{invoice.CAE}</strong> (Vto: {invoice.CAEExpirationDate:dd/MM/yyyy})</p><p>Gracias por tu compra.</p>";
                    string fileName = $"Factura_{invoice.FormattedVoucherNumber}.pdf";

                    await emailService.SendEmailAsync(customerEmail.Trim(), subject, htmlMessage, fileName, pdfBytes);
                    _logger.LogInformation("Factura {Voucher} enviada automáticamente a {Email}", invoice.FormattedVoucherNumber, customerEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo enviar automáticamente la factura {InvoiceId} a {Email}", invoiceId, customerEmail);
                }
            });
        }
    }
}
