using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace GestionQ.Web.Models
{
    public class ConfigurationViewModel
    {
        [Required, Display(Name = "Servidor SQL")]
        public string Server { get; set; } = string.Empty;

        [Required, Display(Name = "Base de Datos")]
        public string Database { get; set; } = string.Empty;

        [Required, Display(Name = "Usuario SQL")]
        public string User { get; set; } = string.Empty;

        [Required, Display(Name = "Contraseña SQL"), DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required, Display(Name = "Nombre Fiscal (Razón Social)")]
        public string CompanyName { get; set; } = string.Empty;

        [Display(Name = "Nombre de Fantasía")]
        public string CompanyFantasyName { get; set; } = string.Empty;

        [Display(Name = "Dirección")]
        public string CompanyAddress { get; set; } = string.Empty;

        [Display(Name = "Teléfono")]
        public string CompanyPhone { get; set; } = string.Empty;

        [Display(Name = "Email de Contacto")]
        public string CompanyEmail { get; set; } = string.Empty;

        [Display(Name = "CUIT de la Empresa")]
        public string CompanyCuit { get; set; } = string.Empty;

        [Display(Name = "Condición frente al IVA")]
        public string CompanyTaxCondition { get; set; } = string.Empty;

        [Display(Name = "Fecha de Inicio de Actividades")]
        [DataType(DataType.Date)]
        public DateTime? CompanyStartOfActivities { get; set; }

        [Display(Name = "Ingresos Brutos (IIBB)")]
        public string CompanyIIBB { get; set; } = string.Empty;

        [Display(Name = "Logo de la Empresa")]
        public IFormFile? LogoFile { get; set; }

        [Display(Name = "Próximo Número Ingreso Proveedor Interno")]
        public int NextInternalSupplierNumber { get; set; }

        [Display(Name = "Ruta de Carpeta JDataGate (Balanza)")]
        public string JDataGateFolderPath { get; set; } = string.Empty;

        [Display(Name = "Tema Visual (Colores)")]
        public string UITheme { get; set; } = "violet";
    }
}
