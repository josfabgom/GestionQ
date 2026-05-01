using System.ComponentModel.DataAnnotations;

namespace GestionQ.Domain.Entities
{
    public class Purchase
    {
        public int Id { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Proveedor")]
        public int SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        [StringLength(50)]
        [Display(Name = "Nro. Factura / Remito")]
        public string? ReferenceNumber { get; set; }

        [StringLength(1)]
        [Display(Name = "Letra")]
        public string? VoucherLetter { get; set; }

        public string? ImageUrl { get; set; }

        public decimal TotalAmount { get; set; }

        public string? Notes { get; set; }
        public PurchaseStatus Status { get; set; } = PurchaseStatus.Received;

        public ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
    }

    public enum PurchaseStatus
    {
        Draft,
        Pending,
        Received,
        Cancelled
    }

    public class PurchaseItem
    {
        public int Id { get; set; }

        public int PurchaseId { get; set; }
        public Purchase? Purchase { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [Required]
        [Display(Name = "Cant. Pedida")]
        public decimal Quantity { get; set; }

        [Display(Name = "Cant. Recibida")]
        public decimal? ReceivedQuantity { get; set; }

        [Required]
        public decimal UnitCost { get; set; }

        public decimal SubTotal => (ReceivedQuantity ?? Quantity) * UnitCost;
    }
}
