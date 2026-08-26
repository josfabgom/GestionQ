using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace GestionQ.Web.Models
{
    public class AfipSettingsViewModel
    {
        [Required(ErrorMessage = "El CUIT es obligatorio")]
        [Display(Name = "CUIT de la Empresa")]
        public string Cuit { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Entorno")]
        public string Environment { get; set; } = "Homologation"; // "Homologation" or "Production"

        [Display(Name = "Certificado (.pfx o .crt)")]
        public IFormFile? CertificateFile { get; set; }

        [Display(Name = "Clave Privada (.key) - Solo si usa .crt")]
        public IFormFile? PrivateKeyFile { get; set; }

        [Display(Name = "Contraseña del PFX - Solo si usa .pfx")]
        public string? CertificatePassword { get; set; }

        public bool HasCertificateConfigured { get; set; }
        public string? ConfiguredCertificateInfo { get; set; }
        public string? ConnectionStatus { get; set; }
        public bool IsConnectionSuccessful { get; set; }
    }
}
