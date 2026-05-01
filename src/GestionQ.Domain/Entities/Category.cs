using System.ComponentModel.DataAnnotations;

namespace GestionQ.Domain.Entities
{
    public class Category // Rubro
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "El nombre del rubro es requerido")]
        [StringLength(100)]
        [Display(Name = "Nombre del Rubro")]
        public string Name { get; set; } = string.Empty;

        public List<SubCategory> SubCategories { get; set; } = new();
    }
}
