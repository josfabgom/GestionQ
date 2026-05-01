using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace GestionQ.Web.Models
{
    public class CustomerViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100)]
        [Display(Name = "Nombre")]
        public string Name { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email inválido")]
        public string? Email { get; set; }

        [Display(Name = "Teléfono")]
        public string? Phone { get; set; }

        [Display(Name = "DNI")]
        public string? Dni { get; set; }

        [Display(Name = "CUIT")]
        public string? Cuit { get; set; }

        [Display(Name = "Condición IVA")]
        public int? TaxConditionId { get; set; }

        [Display(Name = "Dirección")]
        public string? Address { get; set; }

        [Display(Name = "Localidad")]
        public string? Locality { get; set; }

        [Display(Name = "Estado Activo")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Código Interno")]
        public string? InternalCode { get; set; }

        [Display(Name = "Imagen")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Saldo")]
        public decimal Balance { get; set; }

        [Display(Name = "Foto de Perfil")]
        public IFormFile? ImageFile { get; set; }
    }
}
