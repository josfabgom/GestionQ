using System;
using System.ComponentModel.DataAnnotations;

namespace GestionQ.Domain.Entities
{
    public class CashRegisterMovement
    {
        public int Id { get; set; }

        [Required]
        public int CashRegisterId { get; set; }
        public CashRegister? CashRegister { get; set; }

        [Required]
        [Display(Name = "Monto")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(20)]
        [Display(Name = "Tipo de Movimiento")]
        public string Type { get; set; } = string.Empty; // "Ingreso" o "Egreso"

        [Required]
        [MaxLength(200)]
        [Display(Name = "Concepto / Descripción")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Fecha")]
        public DateTime Date { get; set; } = DateTime.Now;
    }
}
