using System;
using System.Collections.Generic;

namespace GestionQ.Domain.Entities
{
    public class Sale
    {
        public int Id { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public decimal TotalAmount { get; set; }

        public string? UserId { get; set; }
        public Microsoft.AspNetCore.Identity.IdentityUser? User { get; set; }

        public int? CashRegisterId { get; set; }
        public CashRegister? CashRegister { get; set; }

        public int? PointOfSaleId { get; set; }
        public PointOfSale? PointOfSale { get; set; }

        public string FormattedTicketNumber => $"{(PointOfSale?.PosNumber ?? 0):D5}-{Id:D8}";

        public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
        public List<SalePayment> Payments { get; set; } = new();
    }

    public class SaleItem
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public Sale? Sale { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class SalePayment
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public Sale? Sale { get; set; }

        public int PaymentMethodId { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }

        public decimal Amount { get; set; }
    }
}
