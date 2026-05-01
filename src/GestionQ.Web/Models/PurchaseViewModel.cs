using System.ComponentModel.DataAnnotations;

namespace GestionQ.Web.Models
{
    public class PurchaseViewModel
    {
        [Required]
        public int SupplierId { get; set; }
        
        public string? ReferenceNumber { get; set; }
        
        public string? VoucherLetter { get; set; }
        
        public string? Notes { get; set; }
        
        public IFormFile? ImageFile { get; set; }
        
        // This will be JSON from the hidden field or parsed manually
        public string ItemsJson { get; set; } = "[]";
    }
}
