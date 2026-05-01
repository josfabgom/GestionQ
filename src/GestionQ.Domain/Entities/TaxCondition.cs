using System.ComponentModel.DataAnnotations;

namespace GestionQ.Domain.Entities
{
    public class TaxCondition
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [Display(Name = "Condición ante el IVA")]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
