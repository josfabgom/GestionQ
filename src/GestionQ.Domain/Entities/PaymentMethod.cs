using System.ComponentModel.DataAnnotations;

namespace GestionQ.Domain.Entities
{
    public class PaymentMethod
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        [Display(Name = "Medio de Pago")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Habilitado")]
        public bool IsActive { get; set; } = true;
    }
}
