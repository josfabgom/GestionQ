using System.ComponentModel.DataAnnotations;

namespace GestionQ.Domain.Entities
{
    public class SubCategory // Subrubro
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del subrubro es requerido")]
        [StringLength(100)]
        [Display(Name = "Nombre del Subrubro")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Rubro")]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public List<Product> Products { get; set; } = new();
    }
}
