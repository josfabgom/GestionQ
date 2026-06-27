using System.ComponentModel.DataAnnotations;

namespace GestionQ.Domain.Entities
{
    public class PointOfSale
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Nombre del Punto de Venta")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Nro. de Punto de Venta (AFIP/Fiscal)")]
        public int PosNumber { get; set; }

        [Display(Name = "Nombre de la PC / Terminal")]
        public string? MachineName { get; set; }

        [Display(Name = "Descripción/Ubicación")]
        public string? Description { get; set; }

        [Display(Name = "Impresora de Tickets Predeterminada")]
        public string? PrinterName { get; set; }

        [Display(Name = "Número de Copias")]
        public int PrintCopies { get; set; } = 1;

        public bool IsActive { get; set; } = true;

        public List<CashRegister> CashRegisters { get; set; } = new();
        public List<Sale> Sales { get; set; } = new();
    }
}
