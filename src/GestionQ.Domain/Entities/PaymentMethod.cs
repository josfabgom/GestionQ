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

        [Display(Name = "Porcentaje de Descuento (%)")]
        public decimal DiscountPercentage { get; set; } = 0;

        [Display(Name = "Descuento Vigente Desde")]
        [DataType(DataType.Date)]
        public DateTime? DiscountValidFrom { get; set; }

        [Display(Name = "Descuento Vigente Hasta")]
        [DataType(DataType.Date)]
        public DateTime? DiscountValidTo { get; set; }
    }
}
