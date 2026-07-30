using System;
using System.Collections.Generic;

namespace GestionQ.Web.Models
{
    public class ProductChangesViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<ProductChangeItem> Changes { get; set; } = new List<ProductChangeItem>();
    }

    public class ProductChangeItem
    {
        public int ProductId { get; set; }
        public int InternalCode { get; set; }
        public string? Barcode { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Stock { get; set; }
        public decimal FinalPrice { get; set; }
        
        // Indicates if the price changed in the period
        public bool HasPriceChange { get; set; }
        
        // Indicates if the stock changed in the period
        public bool HasStockChange { get; set; }
    }
}
