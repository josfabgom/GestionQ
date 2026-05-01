using System;
using System.ComponentModel.DataAnnotations;

namespace GestionQ.Domain.Entities
{
    public class ProductPrice
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        [Display(Name = "Costo Base")]
        public decimal BaseCost { get; set; }

        [Display(Name = "Margen (%)")]
        public decimal ProfitMargin { get; set; }

        [Display(Name = "Imp. Interno")]
        public decimal InternalTax { get; set; }

        [Display(Name = "Precio Final")]
        public decimal FinalPrice { get; set; }

        public DateTime UpdateDate { get; set; } = DateTime.Now;
    }
}
