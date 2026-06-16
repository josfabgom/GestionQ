using System;

namespace GestionQ.Domain.Entities
{
    public class ElectronicInvoice
    {
        public int Id { get; set; }
        
        // Link to the sale (nullable if we create a standalone invoice)
        public int? SaleId { get; set; }
        public Sale? Sale { get; set; }
        
        // Point of sale details
        public int PointOfSaleId { get; set; }
        public PointOfSale? PointOfSale { get; set; }
        public int PointOfSaleNumber { get; set; } // e.g. 5
        
        // AFIP voucher details
        public int InvoiceTypeCode { get; set; } // 1=Factura A, 6=Factura B, 11=Factura C, etc.
        public string InvoiceTypeDesc { get; set; } = string.Empty; // e.g. "Factura A"
        public int InvoiceNumber { get; set; } // Sequence number from AFIP
        public DateTime IssueDate { get; set; } = DateTime.Now;
        public int ConceptCode { get; set; } // 1=Productos, 2=Servicios, 3=Ambos
        
        // Client details
        public int DocTypeCode { get; set; } // 80=CUIT, 96=DNI, 99=Consumidor Final Sin Identificar
        public string DocNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerTaxCondition { get; set; } = string.Empty; // e.g. "Responsable Inscripto"
        
        // Totals
        public decimal NetAmount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal ExemptAmount { get; set; }
        public decimal TotalAmount { get; set; }
        
        // AFIP CAE info
        public string CAE { get; set; } = string.Empty;
        public DateTime CAEExpirationDate { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public string? ErrorMessage { get; set; }
        
        // --- ARCA v4.0 (March 2025) Specific Fields ---
        public bool CanMisMonExt { get; set; } // Cancelled in same foreign currency
        public int CondicionIVAReceptorId { get; set; } // VAT receiver condition (RG 5616)
        
        public string FormattedVoucherNumber => $"{PointOfSaleNumber:D5}-{InvoiceNumber:D8}";
    }
}
