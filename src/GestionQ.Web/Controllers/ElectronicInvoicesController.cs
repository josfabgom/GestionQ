using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GestionQ.Infrastructure.Data;
using GestionQ.Domain.Entities;
using GestionQ.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;

using GestionQ.Domain.Constants;

namespace GestionQ.Web.Controllers
{
    [Authorize(Policy = Permissions.ElectronicInvoices.View)]
    public class ElectronicInvoicesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IElectronicInvoicingService _invoicingService;

        public ElectronicInvoicesController(ApplicationDbContext context, IElectronicInvoicingService invoicingService)
        {
            _context = context;
            _invoicingService = invoicingService;
        }

        // GET: ElectronicInvoices
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Spool));
        }

        // GET: ElectronicInvoices/Create
        public async Task<IActionResult> Create(int saleId)
        {
            var sale = await _context.Sales
                .Include(s => s.Customer)
                .ThenInclude(c => c.TaxCondition)
                .Include(s => s.PointOfSale)
                .Include(s => s.Items)
                .ThenInclude(si => si.Product)
                .ThenInclude(p => p.VatRate)
                .FirstOrDefaultAsync(s => s.Id == saleId);

            if (sale == null)
            {
                TempData["ErrorMessage"] = "La venta especificada no existe.";
                return RedirectToAction(nameof(Index));
            }

            if (sale.ElectronicInvoice != null)
            {
                TempData["ErrorMessage"] = "Esta venta ya posee una factura electrónica autorizada.";
                return RedirectToAction(nameof(Index));
            }

            // Calculate amounts
            decimal netAmount = 0;
            decimal vatAmount = 0;
            decimal exemptAmount = 0;

            foreach (var item in sale.Items)
            {
                decimal itemTotal = item.Quantity * item.UnitPrice;
                decimal vatRatePercent = item.Product?.VatRate?.Rate ?? 21.0m;

                if (vatRatePercent == 0)
                {
                    exemptAmount += itemTotal;
                }
                else
                {
                    // Item price in POS includes VAT. We need to de-scale it.
                    decimal net = itemTotal / (1 + (vatRatePercent / 100));
                    decimal vat = itemTotal - net;
                    netAmount += net;
                    vatAmount += vat;
                }
            }

            // Determine default AFIP values based on customer tax condition
            int defaultInvoiceTypeCode = 6; // Factura B by default
            int defaultCondicionIvaReceptor = 5; // Consumidor Final by default
            string customerCuit = sale.Customer?.Cuit ?? string.Empty;
            string customerDni = sale.Customer?.Dni ?? string.Empty;
            int docTypeCode = 99; // Sin identificar

            if (!string.IsNullOrWhiteSpace(customerCuit))
            {
                docTypeCode = 80; // CUIT
            }
            else if (!string.IsNullOrWhiteSpace(customerDni))
            {
                docTypeCode = 96; // DNI
            }

            if (sale.Customer?.TaxCondition != null)
            {
                var taxConditionName = sale.Customer.TaxCondition.Name.ToLower();
                if (taxConditionName.Contains("inscripto"))
                {
                    defaultInvoiceTypeCode = 1; // Factura A
                    defaultCondicionIvaReceptor = 1; // Responsable Inscripto
                }
                else if (taxConditionName.Contains("monotributo") || taxConditionName.Contains("monotributista"))
                {
                    defaultInvoiceTypeCode = 6; // Factura B
                    defaultCondicionIvaReceptor = 6; // Responsable Monotributo
                }
                else if (taxConditionName.Contains("exento"))
                {
                    defaultInvoiceTypeCode = 6; // Factura B
                    defaultCondicionIvaReceptor = 4; // Sujeto Exento
                }
            }

            var posId = sale.PointOfSaleId ?? 0;
            var posNumber = sale.PointOfSale?.PosNumber ?? 1;
            var posName = sale.PointOfSale?.Name ?? "Punto de Venta por Defecto";

            if (posId == 0)
            {
                var defaultPos = await _context.PointsOfSale.FirstOrDefaultAsync();
                if (defaultPos != null)
                {
                    posId = defaultPos.Id;
                    posNumber = defaultPos.PosNumber;
                    posName = defaultPos.Name;
                }
            }

            var viewModel = new ElectronicInvoiceViewModel
            {
                SaleId = sale.Id,
                PointOfSaleId = posId,
                PointOfSaleNumber = posNumber,
                PointOfSaleName = posName,
                
                CustomerName = sale.Customer?.Name ?? "Consumidor Final",
                DocTypeCode = docTypeCode,
                DocNumber = docTypeCode == 80 ? customerCuit : (docTypeCode == 96 ? customerDni : string.Empty),
                CustomerTaxCondition = sale.Customer?.TaxCondition?.Name ?? "Consumidor Final",
                
                NetAmount = Math.Round(netAmount, 2),
                VatAmount = Math.Round(vatAmount, 2),
                ExemptAmount = Math.Round(exemptAmount, 2),
                TotalAmount = Math.Round(sale.TotalAmount, 2),
                
                ConceptCode = 1, // Productos
                InvoiceTypeCode = defaultInvoiceTypeCode,
                CondicionIVAReceptorId = defaultCondicionIvaReceptor,
                CanMisMonExt = false
            };

            ConfigureViewBags();
            return View(viewModel);
        }

        // POST: ElectronicInvoices/Create
        [HttpPost]
        [Authorize(Policy = Permissions.ElectronicInvoices.Create)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ElectronicInvoiceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ConfigureViewBags();
                return View(model);
            }

            // Check if sale has already been invoiced
            var existingInvoice = await _context.ElectronicInvoices
                .AnyAsync(e => e.SaleId == model.SaleId && e.Status == "Approved");

            if (existingInvoice)
            {
                TempData["ErrorMessage"] = "Esta venta ya posee una factura electrónica autorizada.";
                return RedirectToAction(nameof(Index));
            }

            // Call Electronic Invoicing Service
            if (model.PointOfSaleId == 0)
            {
                var defaultPos = await _context.PointsOfSale.FirstOrDefaultAsync();
                if (defaultPos != null)
                {
                    model.PointOfSaleId = defaultPos.Id;
                    model.PointOfSaleNumber = defaultPos.PosNumber;
                }
            }

            var request = new ElectronicInvoiceRequest
            {
                PointOfSaleId = model.PointOfSaleId,
                PointOfSaleNumber = model.PointOfSaleNumber,
                InvoiceTypeCode = model.InvoiceTypeCode,
                ConceptCode = model.ConceptCode,
                DocTypeCode = model.DocTypeCode,
                DocNumber = model.DocNumber ?? string.Empty,
                CustomerName = model.CustomerName,
                CustomerTaxCondition = model.CustomerTaxCondition,
                NetAmount = model.NetAmount,
                VatAmount = model.VatAmount,
                ExemptAmount = model.ExemptAmount,
                TotalAmount = model.TotalAmount,
                CanMisMonExt = model.CanMisMonExt,
                CondicionIVAReceptorId = model.CondicionIVAReceptorId
            };

            var serviceResponse = await _invoicingService.RequestCAEAsync(request);

            if (serviceResponse.Success)
            {
                // Create Electronic Invoice record
                var invoice = new ElectronicInvoice
                {
                    SaleId = model.SaleId,
                    PointOfSaleId = model.PointOfSaleId,
                    PointOfSaleNumber = model.PointOfSaleNumber,
                    InvoiceTypeCode = model.InvoiceTypeCode,
                    InvoiceTypeDesc = GetInvoiceTypeDesc(model.InvoiceTypeCode),
                    InvoiceNumber = serviceResponse.InvoiceNumber,
                    IssueDate = DateTime.Now,
                    ConceptCode = model.ConceptCode,
                    DocTypeCode = model.DocTypeCode,
                    DocNumber = model.DocNumber ?? string.Empty,
                    CustomerName = model.CustomerName,
                    CustomerTaxCondition = model.CustomerTaxCondition,
                    NetAmount = model.NetAmount,
                    VatAmount = model.VatAmount,
                    ExemptAmount = model.ExemptAmount,
                    TotalAmount = model.TotalAmount,
                    CAE = serviceResponse.CAE,
                    CAEExpirationDate = serviceResponse.CAEExpirationDate,
                    Status = "Approved",
                    CanMisMonExt = model.CanMisMonExt,
                    CondicionIVAReceptorId = model.CondicionIVAReceptorId
                };

                _context.ElectronicInvoices.Add(invoice);
                await _context.SaveChangesAsync();

                // Link to Sale (Update sale entity)
                var sale = await _context.Sales.FindAsync(model.SaleId);
                if (sale != null)
                {
                    sale.ElectronicInvoice = invoice;
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = $"Factura Electrónica {invoice.InvoiceTypeDesc} N° {invoice.FormattedVoucherNumber} autorizada con éxito. CAE: {invoice.CAE}";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                // Authorization failed
                foreach (var err in serviceResponse.Errors)
                {
                    ModelState.AddModelError("", err);
                }
                ConfigureViewBags();
                return View(model);
            }
        }

        // GET: ElectronicInvoices/Print/5
        public async Task<IActionResult> Print(int id)
        {
            var invoice = await _context.ElectronicInvoices
                .Include(e => e.PointOfSale)
                .Include(e => e.Sale)
                .ThenInclude(s => s.Items)
                .ThenInclude(si => si.Product)
                .ThenInclude(p => p.VatRate)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            // Generate barcode string (Standard AFIP CUIT emisor + Tipo Cbte + POS + CAE + Expiration Date + Digito Verificador)
            // standard barcode text format for AFIP standard Code 39 or I2of5: 34-character string
            string cuitEmisor = "30712345678"; // Simulated company CUIT
            string voucherType = invoice.InvoiceTypeCode.ToString("D2");
            string pos = invoice.PointOfSaleNumber.ToString("D4");
            string cae = invoice.CAE;
            string vto = invoice.CAEExpirationDate.ToString("yyyyMMdd");
            
            string rawBarcode = cuitEmisor + voucherType + pos + cae + vto;
            int checkDigit = CalculateBarcodeCheckDigit(rawBarcode);
            invoice.ErrorMessage = rawBarcode + checkDigit; // Store in temp field or read directly

            return View(invoice);
        }

        // GET: ElectronicInvoices/PrintTicket/5
        public async Task<IActionResult> PrintTicket(int id)
        {
            var invoice = await _context.ElectronicInvoices
                .Include(e => e.PointOfSale)
                .Include(e => e.Sale)
                .ThenInclude(s => s.Items)
                .ThenInclude(si => si.Product)
                .ThenInclude(p => p.VatRate)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }

        // GET: ElectronicInvoices/Certificates
        public IActionResult Certificates()
        {
            var certsFolder = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Certificados");
            var keyPath = System.IO.Path.Combine(certsFolder, "private.key");
            var crtPath = System.IO.Path.Combine(certsFolder, "certificate.crt");
            var csrPath = System.IO.Path.Combine(certsFolder, "request.csr");

            var model = new CertificatesViewModel
            {
                HasPrivateKey = System.IO.File.Exists(keyPath),
                HasCertificate = System.IO.File.Exists(crtPath),
                HasCsr = System.IO.File.Exists(csrPath),
                PrivateKeyDate = System.IO.File.Exists(keyPath) ? (DateTime?)System.IO.File.GetCreationTime(keyPath) : null,
                CertificateDate = System.IO.File.Exists(crtPath) ? (DateTime?)System.IO.File.GetCreationTime(crtPath) : null,
                CsrDate = System.IO.File.Exists(csrPath) ? (DateTime?)System.IO.File.GetCreationTime(csrPath) : null
            };

            if (model.HasCertificate)
            {
                try
                {
                    using (var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(crtPath))
                    {
                        model.CertificateSubject = cert.Subject;
                        model.CertificateIssuer = cert.Issuer;
                        model.CertificateExpiration = cert.NotAfter;
                    }
                }
                catch (Exception ex)
                {
                    model.CertificateSubject = "Error al leer certificado: " + ex.Message;
                }
            }

            return View(model);
        }

        // POST: ElectronicInvoices/GenerateCsr
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateCsr(string companyName, string cuit, string commonName)
        {
            if (string.IsNullOrWhiteSpace(companyName) || string.IsNullOrWhiteSpace(cuit) || string.IsNullOrWhiteSpace(commonName))
            {
                TempData["ErrorMessage"] = "Todos los campos son obligatorios para generar la solicitud.";
                return RedirectToAction(nameof(Certificates));
            }

            // Simple validation of CUIT format
            var cleanCuit = cuit.Replace("-", "").Trim();
            if (cleanCuit.Length != 11 || !cleanCuit.All(char.IsDigit))
            {
                TempData["ErrorMessage"] = "La CUIT debe tener exactamente 11 dígitos numéricos.";
                return RedirectToAction(nameof(Certificates));
            }

            try
            {
                var certsFolder = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Certificados");
                if (!System.IO.Directory.Exists(certsFolder))
                {
                    System.IO.Directory.CreateDirectory(certsFolder);
                }

                var keyPath = System.IO.Path.Combine(certsFolder, "private.key");
                var csrPath = System.IO.Path.Combine(certsFolder, "request.csr");

                using (var rsa = System.Security.Cryptography.RSA.Create(2048))
                {
                    // 1. Export private key bytes and save it in standard PEM format
                    var privateKeyBytes = rsa.ExportPkcs8PrivateKey();
                    var privateKeyBase64 = Convert.ToBase64String(privateKeyBytes, Base64FormattingOptions.InsertLineBreaks);
                    var privateKeyPem = $"-----BEGIN PRIVATE KEY-----\r\n{privateKeyBase64}\r\n-----END PRIVATE KEY-----";
                    await System.IO.File.WriteAllTextAsync(keyPath, privateKeyPem);

                    // 2. Generate Certificate Request (CSR)
                    // AFIP requires SerialNumber with CUIT format, Organización (O), and CommonName (CN)
                    var dnString = $"C=AR, O={companyName.Trim()}, CN={commonName.Trim()}, SERIALNUMBER=CUIT {cleanCuit}";
                    var subjectName = new System.Security.Cryptography.X509Certificates.X500DistinguishedName(dnString);
                    
                    var request = new System.Security.Cryptography.X509Certificates.CertificateRequest(
                        subjectName, 
                        rsa, 
                        System.Security.Cryptography.HashAlgorithmName.SHA256, 
                        System.Security.Cryptography.RSASignaturePadding.Pkcs1);

                    byte[] csrDer = request.CreateSigningRequest();
                    var csrBase64 = Convert.ToBase64String(csrDer, Base64FormattingOptions.InsertLineBreaks);
                    var csrPem = $"-----BEGIN CERTIFICATE REQUEST-----\r\n{csrBase64}\r\n-----END CERTIFICATE REQUEST-----";
                    await System.IO.File.WriteAllTextAsync(csrPath, csrPem);
                }

                TempData["SuccessMessage"] = "Clave privada y Solicitud de Certificado (CSR) generadas con éxito. Ahora puede descargar el CSR.";
                return RedirectToAction(nameof(Certificates));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al generar las credenciales de AFIP: {ex.Message}";
                return RedirectToAction(nameof(Certificates));
            }
        }

        // GET: ElectronicInvoices/DownloadCsr
        public IActionResult DownloadCsr()
        {
            var certsFolder = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Certificados");
            var csrPath = System.IO.Path.Combine(certsFolder, "request.csr");

            if (!System.IO.File.Exists(csrPath))
            {
                TempData["ErrorMessage"] = "No se ha generado ninguna solicitud (CSR) aún.";
                return RedirectToAction(nameof(Certificates));
            }

            byte[] fileBytes = System.IO.File.ReadAllBytes(csrPath);
            return File(fileBytes, "application/octet-stream", "gestionq_afip.csr");
        }

        // POST: ElectronicInvoices/UploadCertificate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadCertificate(Microsoft.AspNetCore.Http.IFormFile certificateFile)
        {
            if (certificateFile == null || certificateFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Por favor, seleccione un archivo de certificado válido (.crt).";
                return RedirectToAction(nameof(Certificates));
            }

            try
            {
                var certsFolder = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Certificados");
                if (!System.IO.Directory.Exists(certsFolder))
                {
                    System.IO.Directory.CreateDirectory(certsFolder);
                }

                var crtPath = System.IO.Path.Combine(certsFolder, "certificate.crt");

                using (var stream = new System.IO.FileStream(crtPath, System.IO.FileMode.Create))
                {
                    await certificateFile.CopyToAsync(stream);
                }

                // Verify the uploaded certificate structure
                using (var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(crtPath))
                {
                    TempData["SuccessMessage"] = $"Certificado AFIP cargado con éxito. Emitido para: {cert.Subject}. Vence el {cert.NotAfter:dd/MM/yyyy}.";
                }

                return RedirectToAction(nameof(Certificates));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"El archivo de certificado no es válido: {ex.Message}";
                return RedirectToAction(nameof(Certificates));
            }
        }

        // POST: ElectronicInvoices/DeleteCertificates
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCertificates()
        {
            try
            {
                var certsFolder = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Certificados");
                var keyPath = System.IO.Path.Combine(certsFolder, "private.key");
                var crtPath = System.IO.Path.Combine(certsFolder, "certificate.crt");
                var csrPath = System.IO.Path.Combine(certsFolder, "request.csr");

                if (System.IO.File.Exists(keyPath)) System.IO.File.Delete(keyPath);
                if (System.IO.File.Exists(crtPath)) System.IO.File.Delete(crtPath);
                if (System.IO.File.Exists(csrPath)) System.IO.File.Delete(csrPath);

                TempData["SuccessMessage"] = "Las credenciales y solicitudes locales han sido eliminadas.";
                return RedirectToAction(nameof(Certificates));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al eliminar credenciales: {ex.Message}";
                return RedirectToAction(nameof(Certificates));
            }
        }

        private int CalculateBarcodeCheckDigit(string barcode)
        {
            int sumEven = 0;
            int sumOdd = 0;

            for (int i = 0; i < barcode.Length; i++)
            {
                int digit = barcode[i] - '0';
                if (i % 2 == 0) // Even index (0, 2, 4...) -> Odd AFIP positions (1st, 3rd, 5th...)
                {
                    sumOdd += digit;
                }
                else // Odd index -> Even AFIP positions
                {
                    sumEven += digit;
                }
            }

            int step1 = sumOdd * 3;
            int step2 = step1 + sumEven;
            int remainder = step2 % 10;
            
            return remainder == 0 ? 0 : 10 - remainder;
        }

        private string GetInvoiceTypeDesc(int code)
        {
            return code switch
            {
                1 => "Factura A",
                6 => "Factura B",
                11 => "Factura C",
                _ => "Factura"
            };
        }

        private void ConfigureViewBags()
        {
            ViewBag.VoucherTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "6", Text = "Factura B" },
                new SelectListItem { Value = "1", Text = "Factura A" },
                new SelectListItem { Value = "11", Text = "Factura C" }
            };

            ViewBag.DocTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "99", Text = "Sin Identificar (Consumidor Final)" },
                new SelectListItem { Value = "80", Text = "CUIT" },
                new SelectListItem { Value = "96", Text = "DNI" }
            };

            ViewBag.CondicionesIva = new List<SelectListItem>
            {
                new SelectListItem { Value = "5", Text = "Consumidor Final" },
                new SelectListItem { Value = "1", Text = "IVA Responsable Inscripto" },
                new SelectListItem { Value = "6", Text = "Responsable Monotributo" },
                new SelectListItem { Value = "4", Text = "IVA Sujeto Exento" }
            };

            ViewBag.Conceptos = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "Productos" },
                new SelectListItem { Value = "2", Text = "Servicios" },
                new SelectListItem { Value = "3", Text = "Productos y Servicios" }
            };
        }

        // GET: ElectronicInvoices/Spool
        public async Task<IActionResult> Spool()
        {
            var model = new ElectronicInvoicesDashboardViewModel
            {
                // Invoices already generated and authorized
                AuthorizedInvoices = await _context.ElectronicInvoices
                    .Include(e => e.PointOfSale)
                    .Include(e => e.Sale)
                    .ThenInclude(s => s.Payments)
                    .ThenInclude(p => p.PaymentMethod)
                    .OrderByDescending(e => e.IssueDate)
                    .ToListAsync(),

                // Sales that have NOT yet been electronically invoiced
                PendingSales = await _context.Sales
                    .Include(s => s.Customer)
                    .Include(s => s.PointOfSale)
                    .Include(s => s.Payments)
                    .ThenInclude(p => p.PaymentMethod)
                    .Where(s => s.ElectronicInvoice == null)
                    .OrderByDescending(s => s.Date)
                    .ToListAsync()
            };

            // Calculate simple dashboard metrics
            model.TotalInvoiced = model.AuthorizedInvoices
                .Where(e => e.Status == "Approved")
                .Sum(e => e.TotalAmount);
            
            model.ApprovedCount = model.AuthorizedInvoices
                .Count(e => e.Status == "Approved");
            
            model.PendingCount = model.PendingSales.Count;

            return View(model);
        }

        // POST: ElectronicInvoices/QuickInvoice
        [HttpPost]
        public async Task<IActionResult> QuickInvoice(int saleId)
        {
            var sale = await _context.Sales
                .Include(s => s.Customer)
                .ThenInclude(c => c.TaxCondition)
                .Include(s => s.PointOfSale)
                .Include(s => s.Items)
                .ThenInclude(si => si.Product)
                .ThenInclude(p => p.VatRate)
                .FirstOrDefaultAsync(s => s.Id == saleId);

            if (sale == null)
            {
                return Json(new { success = false, message = "La venta especificada no existe." });
            }

            if (sale.ElectronicInvoice != null && sale.ElectronicInvoice.Status == "Approved")
            {
                return Json(new { success = true, message = "Esta venta ya posee una factura electrónica autorizada.", cae = sale.ElectronicInvoice.CAE });
            }

            // Calculate amounts
            decimal netAmount = 0;
            decimal vatAmount = 0;
            decimal exemptAmount = 0;

            foreach (var item in sale.Items)
            {
                decimal itemTotal = item.Quantity * item.UnitPrice;
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

            // Determine default AFIP values
            int defaultInvoiceTypeCode = 6; // Factura B
            int defaultCondicionIvaReceptor = 5; // Consumidor Final
            string customerCuit = sale.Customer?.Cuit ?? string.Empty;
            string customerDni = sale.Customer?.Dni ?? string.Empty;
            int docTypeCode = 99; // Sin identificar

            if (!string.IsNullOrWhiteSpace(customerCuit))
            {
                docTypeCode = 80;
            }
            else if (!string.IsNullOrWhiteSpace(customerDni))
            {
                docTypeCode = 96;
            }

            if (sale.Customer?.TaxCondition != null)
            {
                var taxConditionName = sale.Customer.TaxCondition.Name.ToLower();
                if (taxConditionName.Contains("inscripto"))
                {
                    defaultInvoiceTypeCode = 1; // Factura A
                    defaultCondicionIvaReceptor = 1; // Responsable Inscripto
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
                ConceptCode = 1, // Productos
                DocTypeCode = docTypeCode,
                DocNumber = docTypeCode == 80 ? customerCuit : (docTypeCode == 96 ? customerDni : string.Empty),
                CustomerName = sale.Customer?.Name ?? "Consumidor Final",
                CustomerTaxCondition = sale.Customer?.TaxCondition?.Name ?? "Consumidor Final",
                NetAmount = Math.Round(netAmount, 2),
                VatAmount = Math.Round(vatAmount, 2),
                ExemptAmount = Math.Round(exemptAmount, 2),
                TotalAmount = Math.Round(sale.TotalAmount, 2),
                CanMisMonExt = false,
                CondicionIVAReceptorId = defaultCondicionIvaReceptor
            };

            var serviceResponse = await _invoicingService.RequestCAEAsync(request);

            if (serviceResponse.Success)
            {
                var invoice = new ElectronicInvoice
                {
                    SaleId = sale.Id,
                    PointOfSaleId = request.PointOfSaleId,
                    PointOfSaleNumber = request.PointOfSaleNumber,
                    InvoiceTypeCode = request.InvoiceTypeCode,
                    InvoiceTypeDesc = GetInvoiceTypeDesc(request.InvoiceTypeCode),
                    InvoiceNumber = serviceResponse.InvoiceNumber,
                    IssueDate = DateTime.Now,
                    ConceptCode = request.ConceptCode,
                    DocTypeCode = request.DocTypeCode,
                    DocNumber = request.DocNumber,
                    CustomerName = request.CustomerName,
                    CustomerTaxCondition = request.CustomerTaxCondition,
                    NetAmount = request.NetAmount,
                    VatAmount = request.VatAmount,
                    ExemptAmount = request.ExemptAmount,
                    TotalAmount = request.TotalAmount,
                    CAE = serviceResponse.CAE,
                    CAEExpirationDate = serviceResponse.CAEExpirationDate,
                    Status = "Approved",
                    CanMisMonExt = false,
                    CondicionIVAReceptorId = request.CondicionIVAReceptorId
                };

                _context.ElectronicInvoices.Add(invoice);
                sale.ElectronicInvoice = invoice;
                
                // Automatically add a pending print job to the spool
                var printJob = new FiscalPrintJob
                {
                    SaleId = sale.Id,
                    CreatedDate = DateTime.Now,
                    Status = "Pending",
                    PrinterName = "Impresora Fiscal Térmica"
                };
                _context.FiscalPrintJobs.Add(printJob);

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Factura {invoice.InvoiceTypeDesc} N° {invoice.FormattedVoucherNumber} autorizada con éxito.", 
                    cae = invoice.CAE,
                    voucherNumber = invoice.FormattedVoucherNumber
                });
            }
            else
            {
                var errors = string.Join("; ", serviceResponse.Errors);
                return Json(new { success = false, message = "ARCA rechazó la solicitud: " + errors });
            }
        }

        // POST: ElectronicInvoices/PrintSpoolJob
        [HttpPost]
        public async Task<IActionResult> PrintSpoolJob(int saleId)
        {
            var sale = await _context.Sales
                .Include(s => s.ElectronicInvoice)
                .FirstOrDefaultAsync(s => s.Id == saleId);

            if (sale == null)
            {
                return Json(new { success = false, message = "La venta no existe." });
            }

            if (sale.ElectronicInvoice == null || sale.ElectronicInvoice.Status != "Approved")
            {
                return Json(new { success = false, message = "La venta debe estar facturada ante ARCA para imprimir un comprobante fiscal." });
            }

            // Find or create job
            var job = await _context.FiscalPrintJobs
                .FirstOrDefaultAsync(j => j.SaleId == saleId);

            if (job == null)
            {
                job = new FiscalPrintJob
                {
                    SaleId = saleId,
                    CreatedDate = DateTime.Now,
                    Status = "Pending"
                };
                _context.FiscalPrintJobs.Add(job);
            }

            // Simulating printer processing (delay of 1.2s to make it feel authentic)
            await Task.Delay(1200);

            try
            {
                // Simulate success printing
                job.Status = "Printed";
                job.PrintedDate = DateTime.Now;
                job.PrintedCount += 1;
                job.ErrorMessage = null;

                _context.FiscalPrintJobs.Update(job);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Impresión fiscal completada con éxito.", printedDate = job.PrintedDate?.ToString("dd/MM/yyyy HH:mm:ss") });
            }
            catch (Exception ex)
            {
                job.Status = "Failed";
                job.ErrorMessage = ex.Message;
                _context.FiscalPrintJobs.Update(job);
                await _context.SaveChangesAsync();

                return Json(new { success = false, message = "Error de hardware / impresora: " + ex.Message });
            }
        }

        // POST: ElectronicInvoices/BatchInvoice
        [HttpPost]
        public async Task<IActionResult> BatchInvoice([FromBody] List<int> saleIds)
        {
            if (saleIds == null || !saleIds.Any())
            {
                return Json(new { success = false, message = "No se seleccionaron ventas para facturar." });
            }

            int successCount = 0;
            List<string> failures = new();

            foreach (var saleId in saleIds)
            {
                try
                {
                    var sale = await _context.Sales
                        .Include(s => s.Customer)
                        .ThenInclude(c => c.TaxCondition)
                        .Include(s => s.PointOfSale)
                        .Include(s => s.Items)
                        .ThenInclude(si => si.Product)
                        .ThenInclude(p => p.VatRate)
                        .FirstOrDefaultAsync(s => s.Id == saleId);

                    if (sale == null || (sale.ElectronicInvoice != null && sale.ElectronicInvoice.Status == "Approved"))
                    {
                        continue;
                    }

                    // Amounts
                    decimal netAmount = 0;
                    decimal vatAmount = 0;
                    decimal exemptAmount = 0;

                    foreach (var item in sale.Items)
                    {
                        decimal itemTotal = item.Quantity * item.UnitPrice;
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

                    // Defaults
                    int defaultInvoiceTypeCode = 6;
                    int defaultCondicionIvaReceptor = 5;
                    string customerCuit = sale.Customer?.Cuit ?? string.Empty;
                    string customerDni = sale.Customer?.Dni ?? string.Empty;
                    int docTypeCode = 99;

                    if (!string.IsNullOrWhiteSpace(customerCuit)) docTypeCode = 80;
                    else if (!string.IsNullOrWhiteSpace(customerDni)) docTypeCode = 96;

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

                    var request = new ElectronicInvoiceRequest
                    {
                        PointOfSaleId = sale.PointOfSaleId ?? 0,
                        PointOfSaleNumber = sale.PointOfSale?.PosNumber ?? 1,
                        InvoiceTypeCode = defaultInvoiceTypeCode,
                        ConceptCode = 1,
                        DocTypeCode = docTypeCode,
                        DocNumber = docTypeCode == 80 ? customerCuit : (docTypeCode == 96 ? customerDni : string.Empty),
                        CustomerName = sale.Customer?.Name ?? "Consumidor Final",
                        CustomerTaxCondition = sale.Customer?.TaxCondition?.Name ?? "Consumidor Final",
                        NetAmount = Math.Round(netAmount, 2),
                        VatAmount = Math.Round(vatAmount, 2),
                        ExemptAmount = Math.Round(exemptAmount, 2),
                        TotalAmount = Math.Round(sale.TotalAmount, 2),
                        CanMisMonExt = false,
                        CondicionIVAReceptorId = defaultCondicionIvaReceptor
                    };

                    var serviceResponse = await _invoicingService.RequestCAEAsync(request);

                    if (serviceResponse.Success)
                    {
                        var invoice = new ElectronicInvoice
                        {
                            SaleId = sale.Id,
                            PointOfSaleId = request.PointOfSaleId,
                            PointOfSaleNumber = request.PointOfSaleNumber,
                            InvoiceTypeCode = request.InvoiceTypeCode,
                            InvoiceTypeDesc = GetInvoiceTypeDesc(request.InvoiceTypeCode),
                            InvoiceNumber = serviceResponse.InvoiceNumber,
                            IssueDate = DateTime.Now,
                            ConceptCode = request.ConceptCode,
                            DocTypeCode = request.DocTypeCode,
                            DocNumber = request.DocNumber,
                            CustomerName = request.CustomerName,
                            CustomerTaxCondition = request.CustomerTaxCondition,
                            NetAmount = request.NetAmount,
                            VatAmount = request.VatAmount,
                            ExemptAmount = request.ExemptAmount,
                            TotalAmount = request.TotalAmount,
                            CAE = serviceResponse.CAE,
                            CAEExpirationDate = serviceResponse.CAEExpirationDate,
                            Status = "Approved",
                            CanMisMonExt = false,
                            CondicionIVAReceptorId = request.CondicionIVAReceptorId
                        };

                        _context.ElectronicInvoices.Add(invoice);
                        sale.ElectronicInvoice = invoice;

                        var printJob = new FiscalPrintJob
                        {
                            SaleId = sale.Id,
                            CreatedDate = DateTime.Now,
                            Status = "Pending"
                        };
                        _context.FiscalPrintJobs.Add(printJob);

                        successCount++;
                    }
                    else
                    {
                        failures.Add($"Venta #{sale.Id}: {string.Join(", ", serviceResponse.Errors)}");
                    }
                }
                catch (Exception ex)
                {
                    failures.Add($"Venta #{saleId}: Error - {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = $"Proceso finalizado. Facturadas con éxito: {successCount}. Fallidas: {failures.Count}.",
                failures = failures 
            });
        }

        // POST: ElectronicInvoices/BatchPrint
        [HttpPost]
        public async Task<IActionResult> BatchPrint([FromBody] List<int> saleIds)
        {
            if (saleIds == null || !saleIds.Any())
            {
                return Json(new { success = false, message = "No se seleccionaron ventas para imprimir." });
            }

            int successCount = 0;
            List<string> failures = new();

            // Simulating sequential spool printing with small delays
            foreach (var saleId in saleIds)
            {
                try
                {
                    var sale = await _context.Sales
                        .Include(s => s.ElectronicInvoice)
                        .FirstOrDefaultAsync(s => s.Id == saleId);

                    if (sale == null || sale.ElectronicInvoice == null || sale.ElectronicInvoice.Status != "Approved")
                    {
                        failures.Add($"Venta #{saleId}: No posee factura electrónica aprobada.");
                        continue;
                    }

                    var job = await _context.FiscalPrintJobs
                        .FirstOrDefaultAsync(j => j.SaleId == saleId);

                    if (job == null)
                    {
                        job = new FiscalPrintJob
                        {
                            SaleId = saleId,
                            CreatedDate = DateTime.Now,
                            Status = "Pending"
                        };
                        _context.FiscalPrintJobs.Add(job);
                    }

                    await Task.Delay(400); // simulation delay per document

                    job.Status = "Printed";
                    job.PrintedDate = DateTime.Now;
                    job.PrintedCount += 1;
                    job.ErrorMessage = null;

                    _context.FiscalPrintJobs.Update(job);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failures.Add($"Venta #{saleId}: Error al imprimir - {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = $"Impresión por lote completada. Impresos con éxito: {successCount}. Fallidos: {failures.Count}.",
                failures = failures
            });
        }
    }

    // View Models
    public class ElectronicInvoicesDashboardViewModel
    {
        public List<ElectronicInvoice> AuthorizedInvoices { get; set; } = new();
        public List<Sale> PendingSales { get; set; } = new();
        
        public decimal TotalInvoiced { get; set; }
        public int ApprovedCount { get; set; }
        public int PendingCount { get; set; }
    }

    public class ElectronicInvoiceViewModel
    {
        public int SaleId { get; set; }
        
        public int PointOfSaleId { get; set; }
        public int PointOfSaleNumber { get; set; }
        public string PointOfSaleName { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;
        public int DocTypeCode { get; set; }
        public string? DocNumber { get; set; }
        public string CustomerTaxCondition { get; set; } = string.Empty;

        public decimal NetAmount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal ExemptAmount { get; set; }
        public decimal TotalAmount { get; set; }

        // ARCA v4.0 specific fields
        public int ConceptCode { get; set; }
        public int InvoiceTypeCode { get; set; }
        public int CondicionIVAReceptorId { get; set; }
        public bool CanMisMonExt { get; set; }
    }

    public class CertificatesViewModel
    {
        public bool HasPrivateKey { get; set; }
        public bool HasCertificate { get; set; }
        public bool HasCsr { get; set; }

        public DateTime? PrivateKeyDate { get; set; }
        public DateTime? CertificateDate { get; set; }
        public DateTime? CsrDate { get; set; }

        public string? CertificateSubject { get; set; }
        public string? CertificateIssuer { get; set; }
        public DateTime? CertificateExpiration { get; set; }
    }
}
