using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace GestionQ.Web.Models
{
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Código Interno")]
        public int InternalCode { get; set; }

        [StringLength(50)]
        [Display(Name = "Código (Barras)")]
        public string? Barcode { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100)]
        [Display(Name = "Nombre del Producto")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Subrubro")]
        [Required(ErrorMessage = "Debe seleccionar un subrubro")]
        public int? SubCategoryId { get; set; }

        [Display(Name = "¿Es Pesable?")]
        public bool IsPesable { get; set; }

        [Display(Name = "¿Es Fraccionable?")]
        public bool IsFractionable { get; set; }

        [Display(Name = "Enviar a Balanza")]
        public bool SendToScale { get; set; }

        [Display(Name = "Costo Base")]
        public decimal BaseCost { get; set; }

        [Display(Name = "Margen de Ganancia (%)")]
        public decimal ProfitMargin { get; set; }

        [Display(Name = "Impuesto Interno ($)")]
        public decimal InternalTax { get; set; }

        [Required(ErrorMessage = "El precio es requerido")]
        [Display(Name = "Precio Unitario")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "El stock es requerido")]
        [Display(Name = "Stock Actual")]
        public decimal Stock { get; set; }

        [Display(Name = "Stock Mínimo")]
        public decimal MinimumStock { get; set; }

        [Display(Name = "Habilitado")]
        public bool IsActive { get; set; } = true;
        
        [Display(Name = "Días de Vencimiento")]
        public int ExpirationDays { get; set; } = 0;

        [Display(Name = "Alícuota IVA")]
        public int? VatRateId { get; set; }

        [Display(Name = "Imagen del Producto")]
        public IFormFile? ImageFile { get; set; }
        
        public string? ImageUrl { get; set; }
    }
}
