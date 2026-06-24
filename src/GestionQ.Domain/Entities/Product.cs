using System.ComponentModel.DataAnnotations;

namespace GestionQ.Domain.Entities
{
    public class Product
    {
        public int Id { get; set; }

        [Display(Name = "Código Interno")]
        public int InternalCode { get; set; }

        [StringLength(50)]
        public string? Barcode { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Subrubro")]
        public int? SubCategoryId { get; set; }
        public SubCategory? SubCategory { get; set; }

        [Display(Name = "¿Es Pesable?")]
        public bool IsPesable { get; set; } = false;

        [Display(Name = "¿Es Fraccionable?")]
        public bool IsFractionable { get; set; } = false;

        [Display(Name = "Enviar a Balanza")]
        public bool SendToScale { get; set; } = false;

        [Display(Name = "Fecha Envío Balanza")]
        public DateTime? LastSentToScaleDate { get; set; }

        public decimal Price { get; set; }
        public decimal Stock { get; set; }
        
        [Display(Name = "Stock Mínimo")]
        public decimal MinimumStock { get; set; } = 0;

        public virtual ICollection<ProductPrice> PriceHistory { get; set; } = new List<ProductPrice>();

        [Display(Name = "Alícuota IVA")]
        public int? VatRateId { get; set; }
        public VatRate? VatRate { get; set; }

        public string? ImageUrl { get; set; }

        public DateTime CreationDate { get; set; } = DateTime.Now;
        
        [Display(Name = "Días de Vencimiento")]
        public int ExpirationDays { get; set; } = 0;

        public bool IsDepartment { get; set; } = false;
        public bool IsActive { get; set; } = true;
        
        public bool NeedsLabelPrint { get; set; } = false;
    }
}
