namespace GestionQ.Web.Models
{
    public class ProductSalesSummaryViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Total { get; set; }
    }
}
