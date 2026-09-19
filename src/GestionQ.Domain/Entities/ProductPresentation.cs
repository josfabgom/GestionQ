using System.ComponentModel.DataAnnotations;

namespace GestionQ.Domain.Entities
{
    public class ProductPresentation
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Barcode { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue)]
        public decimal Quantity { get; set; }

        public decimal? Price { get; set; }

        public bool NeedsLabelPrint { get; set; } = false;

        public bool IsBulk { get; set; } = false;

        public bool IsActive { get; set; } = true;
    }
}