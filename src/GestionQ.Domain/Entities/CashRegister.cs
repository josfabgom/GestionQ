using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace GestionQ.Domain.Entities
{
    public class CashRegister
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public IdentityUser? User { get; set; }

        [Display(Name = "Fecha/Hora Apertura")]
        public DateTime OpeningDate { get; set; } = DateTime.Now;

        [Display(Name = "Fecha/Hora Cierre")]
        public DateTime? ClosingDate { get; set; }

        [Display(Name = "Saldo Inicial (Cambio/Efectivo)")]
        public decimal InitialBalance { get; set; }

        [Display(Name = "Total Calculado por Sistema (Efectivo)")]
        public decimal? ExpectedCashBalance { get; set; }

        [Display(Name = "Efectivo Contado al Cierre")]
        public decimal? FinalCashBalance { get; set; }

        [Display(Name = "Diferencia de Efectivo (Sobrante/Faltante)")]
        public decimal? Difference { get; set; }

        public bool IsOpen => ClosingDate == null;

        public List<Sale> Sales { get; set; } = new();
    }
}
