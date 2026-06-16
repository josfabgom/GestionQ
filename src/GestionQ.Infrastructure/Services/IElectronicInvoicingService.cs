using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GestionQ.Infrastructure.Services
{
    public interface IElectronicInvoicingService
    {
        Task<ElectronicInvoiceResponse> RequestCAEAsync(ElectronicInvoiceRequest request);
        Task<int> GetLastAuthorizedVoucherAsync(int posNumber, int voucherTypeCode);
        Task<bool> CheckInfrastructureStatusAsync(); // FEDummy
    }

    public class ElectronicInvoiceRequest
    {
        public int PointOfSaleId { get; set; }
        public int PointOfSaleNumber { get; set; }
        public int InvoiceTypeCode { get; set; } // 1=Factura A, 6=Factura B, 11=Factura C
        public int ConceptCode { get; set; } // 1=Productos, 2=Servicios, 3=Ambos
        public int DocTypeCode { get; set; } // 80=CUIT, 96=DNI, 99=Consumidor Final Sin Identificar
        public string DocNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerTaxCondition { get; set; } = string.Empty;
        
        public decimal NetAmount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal ExemptAmount { get; set; }
        public decimal TotalAmount { get; set; }
        
        // --- ARCA v4.0 Specifications (RG 4291 / RG 5616 - March 2025) ---
        public bool CanMisMonExt { get; set; } // Cancelled in same foreign currency
        public int CondicionIVAReceptorId { get; set; } // VAT receiver condition code (e.g. 1=Resp. Inscripto, 5=Consumidor Final, etc.)
    }

    public class ElectronicInvoiceResponse
    {
        public bool Success { get; set; }
        public string CAE { get; set; } = string.Empty;
        public DateTime CAEExpirationDate { get; set; }
        public int InvoiceNumber { get; set; }
        public string Status { get; set; } = "Rejected"; // Approved, Rejected, Observed
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}
