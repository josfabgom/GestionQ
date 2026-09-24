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

        [Display(Name = "Ngrok Auth Token")]
        public string NgrokAuthToken { get; set; } = string.Empty;

        [Display(Name = "Ngrok Domain")]
        public string NgrokDomain { get; set; } = string.Empty;

        [Display(Name = "URL del Dashboard (Nube)")]
        public string VpsSyncUrl { get; set; } = string.Empty;

        [Display(Name = "API Key")]
        public string DashboardApiKey { get; set; } = string.Empty;

        [Display(Name = "Medio de Pago por Defecto en POS")]
        public int? DefaultPaymentMethodId { get; set; }

        [Display(Name = "Servidor SMTP (Ej: smtp.gmail.com)")]
        public string SmtpHost { get; set; } = string.Empty;

        [Display(Name = "Puerto SMTP (Ej: 587)")]
        public string SmtpPort { get; set; } = string.Empty;

        [Display(Name = "Correo Remitente")]
        public string SmtpEmail { get; set; } = string.Empty;

        [Display(Name = "Contraseña SMTP")]
        public string SmtpPassword { get; set; } = string.Empty;

        [Display(Name = "Activar Subida FTP")]
        public bool CloudFtpEnabled { get; set; }

        [Display(Name = "Servidor FTP (Ej: ftp.midominio.com)")]
        public string CloudFtpHost { get; set; } = string.Empty;

        [Display(Name = "Usuario FTP")]
        public string CloudFtpUser { get; set; } = string.Empty;

        [Display(Name = "Contraseña FTP")]
        public string CloudFtpPassword { get; set; } = string.Empty;

        [Display(Name = "Carpeta Remota FTP (Ej: /public_html/facturas)")]
        public string CloudFtpRemoteFolder { get; set; } = string.Empty;

        [Display(Name = "Dominio Público (Ej: https://midominio.com/facturas/)")]
        public string CloudPublicDomain { get; set; } = string.Empty;

        [Display(Name = "URL del Servidor de Actualizaciones")]
        public string UpdateManifestUrl { get; set; } = string.Empty;
    }
}
