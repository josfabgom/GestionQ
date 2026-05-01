using System.ComponentModel.DataAnnotations;

namespace GestionQ.Domain.Entities
{
    public class Supplier
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del proveedor es requerido")]
        [StringLength(100)]
        [Display(Name = "Nombre / Razón Social")]
        public string Name { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "CUIT/CUIL")]
        public string? TaxId { get; set; }

        [StringLength(100)]
        [Display(Name = "Persona de Contacto")]
        public string? ContactPerson { get; set; }

        [StringLength(100)]
        [Display(Name = "Localidad")]
        public string? City { get; set; }

        [StringLength(100)]
        [Display(Name = "Dirección")]
        public string? Address { get; set; }

        [StringLength(50)]
        [Display(Name = "Teléfono")]
        public string? Phone { get; set; }

        [StringLength(100)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Condición IVA")]
        public int? TaxConditionId { get; set; }
        public TaxCondition? TaxCondition { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    }
}
