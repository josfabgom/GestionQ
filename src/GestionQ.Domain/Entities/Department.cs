using System.ComponentModel.DataAnnotations;

namespace GestionQ.Domain.Entities
{
    public class Department
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del departamento es requerido")]
        [StringLength(100)]
        [Display(Name = "Nombre del Departamento")]
        public string Name { get; set; } = string.Empty;

        [StringLength(10)]
        [Display(Name = "Tecla Rápida")]
        public string? Hotkey { get; set; } // e.g. "F1", "F2", etc.

        [Required(ErrorMessage = "La alícuota de IVA es requerida")]
        [Display(Name = "Alícuota IVA")]
        public int VatRateId { get; set; }
        public VatRate? VatRate { get; set; }

        [Display(Name = "Producto Virtual")]
        public int VirtualProductId { get; set; }
        public Product? VirtualProduct { get; set; }
    }
}
