using System;

namespace GestionQ.Domain.Entities
{
    public class FiscalPrintJob
    {
        public int Id { get; set; }
        
        public int SaleId { get; set; }
        public Sale? Sale { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? PrintedDate { get; set; }
        
        // Status values: "Pending" (needs printing), "Printed" (printed successfully), "Failed" (printer error/cancelled)
        public string Status { get; set; } = "Pending"; 
        
        public string? ErrorMessage { get; set; }
        public int PrintedCount { get; set; }
        
        // Simulates the physical/virtual printer used
        public string PrinterName { get; set; } = "Impresora Fiscal Térmica";
    }
}
