using System.ComponentModel.DataAnnotations;

namespace GestionQ.Domain.Entities
{
    public class SupplierPayment
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Proveedor")]
        public int SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        [Display(Name = "Factura Asignada")]
        public int? PurchaseId { get; set; }
        public Purchase? Purchase { get; set; }

        [Required]
        [Display(Name = "Fecha")]
        public DateTime Date { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Monto")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Medio de Pago")]
        public int PaymentMethodId { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }

        [StringLength(100)]
        [Display(Name = "Comprobante / Nro. Transferencia")]
        public string? ReferenceNumber { get; set; }

        [StringLength(50)]
        [Display(Name = "Nro. de Cheque")]
        public string? CheckNumber { get; set; }

        [StringLength(100)]
        [Display(Name = "Banco del Cheque")]
        public string? CheckBank { get; set; }

        [Display(Name = "Fecha de Vencimiento del Cheque")]
        public DateTime? CheckDueDate { get; set; }

        public string? Notes { get; set; }
    }
}