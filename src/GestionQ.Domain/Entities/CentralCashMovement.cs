using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace GestionQ.Domain.Entities
{
    public class CentralCashMovement
    {
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Fecha")]
        public DateTime Date { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(20)]
        [Display(Name = "Tipo")]
        public string Type { get; set; } = string.Empty; // "Ingreso" o "Egreso"

        [Required]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "Monto")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(200)]
        [Display(Name = "Concepto")]
        public string Concept { get; set; } = string.Empty;

        public string? UserId { get; set; }
        public IdentityUser? User { get; set; }

        // Opcional: Origen (Rendición de caja)
        public int? SourceCashRegisterId { get; set; }
        public CashRegister? SourceCashRegister { get; set; }

        // Opcional: Destino (Recibo de Pago)
        public int? PaymentReceiptId { get; set; }
        public PaymentReceipt? PaymentReceipt { get; set; }
    }
}
