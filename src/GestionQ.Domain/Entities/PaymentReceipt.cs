using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace GestionQ.Domain.Entities
{
    public class PaymentReceipt
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        [Display(Name = "Nro. Recibo / OP")]
        public string ReceiptNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Fecha")]
        public DateTime Date { get; set; } = DateTime.Now;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
        [Display(Name = "Monto")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(200)]
        [Display(Name = "Concepto")]
        public string Concept { get; set; } = string.Empty;

        [Display(Name = "Proveedor (Opcional)")]
        public int? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        public int? PurchaseId { get; set; }
        public Purchase? Purchase { get; set; }

        [Required]
        [Display(Name = "Medio de Pago")]
        public int PaymentMethodId { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }

        [MaxLength(100)]
        public string? ReferenceNumber { get; set; }

        [MaxLength(50)]
        public string? CheckNumber { get; set; }

        [MaxLength(100)]
        public string? CheckBank { get; set; }

        public DateTime? CheckDueDate { get; set; }

        public string? Notes { get; set; }

        public string? UserId { get; set; }
        public IdentityUser? User { get; set; }
    }
}
