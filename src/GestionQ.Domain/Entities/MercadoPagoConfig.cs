using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionQ.Domain.Entities
{
    public class MercadoPagoConfig
    {
        [Key]
        public int Id { get; set; }

        public int? PointOfSaleId { get; set; }

        [Required]
        [StringLength(255)]
        public string AccessToken { get; set; } = string.Empty;

        [StringLength(100)]
        public string ExternalPosId { get; set; } = string.Empty;

        [StringLength(100)]
        public string PointDeviceId { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string DefaultMethod { get; set; } = "QR"; // "QR" o "POINT"

        [StringLength(255)]
        public string? RefreshToken { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public long? MpUserId { get; set; }

        public bool IsActive { get; set; } = true;
        [ForeignKey("PointOfSaleId")]
        public virtual PointOfSale? PointOfSale { get; set; }
    }
}
