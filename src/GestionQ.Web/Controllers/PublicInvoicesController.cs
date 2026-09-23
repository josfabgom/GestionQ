using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using GestionQ.Infrastructure.Data;
using GestionQ.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace GestionQ.Web.Controllers
{
    [AllowAnonymous]
    public class PublicInvoicesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IInvoicePdfGenerator _pdfGenerator;
        private readonly IConfiguration _config;

        public PublicInvoicesController(ApplicationDbContext context, IInvoicePdfGenerator pdfGenerator, IConfiguration config)
        {
            _context = context;
            _pdfGenerator = pdfGenerator;
            _config = config;
        }

        [HttpGet("PublicInvoices/Download/{id:guid}")]
        public async Task<IActionResult> Download(Guid id)
        {
            var invoice = await _context.ElectronicInvoices
                .Include(e => e.PointOfSale)
                .Include(e => e.Sale)
                    .ThenInclude(s => s.Items)
                    .ThenInclude(si => si.Product)
                .FirstOrDefaultAsync(e => e.DownloadGuid == id);

            if (invoice == null || invoice.Status != "Approved")
            {
                return NotFound("Factura no encontrada o no aprobada.");
            }

            string companyName = _config["CompanyInfo:Name"] ?? "GestionQ";
            string companyCuit = _config["CompanyInfo:Cuit"] ?? "";
            string companyCondition = _config["CompanyInfo:TaxCondition"] ?? "";
            string companyAddress = _config["CompanyInfo:Address"] ?? "";

            byte[] pdfBytes = _pdfGenerator.GeneratePdf(invoice, companyName, companyCuit, companyCondition, companyAddress);

            string fileName = $"Factura_{invoice.FormattedVoucherNumber}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}
