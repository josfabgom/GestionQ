using System;
using System.Collections.Generic;

namespace GestionQ.Web.Models
{
    public class StatisticsViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public decimal TotalSalesAmount { get; set; }
        public decimal TotalItemsSold { get; set; }

        public List<ProductSaleStat> SalesByProduct { get; set; } = new();
        public List<PaymentMethodStat> SalesByPaymentMethod { get; set; } = new();
    }

    public class ProductSaleStat
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class PaymentMethodStat
    {
        public int PaymentMethodId { get; set; }
        public string PaymentMethodName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }
}
