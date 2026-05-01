using System.ComponentModel.DataAnnotations;

namespace GestionQ.Domain.Entities
{
    public class VatCondition
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [Display(Name = "Condición / Alícuota")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Porcentaje (%)")]
        public decimal Rate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
