using System.ComponentModel.DataAnnotations;

namespace GestionQ.Domain.Entities
{
    public class VatRate
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [Display(Name = "Nombre de Alícuota")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Porcentaje (%)")]
        public decimal Rate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
