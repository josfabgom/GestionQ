using System.ComponentModel.DataAnnotations;

namespace GestionQ.Domain.Entities
{
    public enum MovementType
    {
        [Display(Name = "Ingreso por Compra")]
        Purchase = 1,
        [Display(Name = "Egreso por Venta")]
        Sale = 2,
        [Display(Name = "Ajuste Positivo")]
        AdjustmentIn = 3,
        [Display(Name = "Ajuste Negativo")]
        AdjustmentOut = 4,
        [Display(Name = "Devolución")]
        Return = 5
    }

    public class StockMovement
    {
        public int Id { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.Now;

        [Required]
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [Required]
        public decimal Quantity { get; set; } // Positive for in, negative for out

        [Required]
        public MovementType Type { get; set; }

        public string? Concept { get; set; } // Reference number or reason

        // Optional links to source documents
        public int? PurchaseId { get; set; }
        public int? SaleId { get; set; }
        
        public decimal PreviousStock { get; set; }
        public decimal NewStock { get; set; }
    }
}
