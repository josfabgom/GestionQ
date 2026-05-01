using System.ComponentModel.DataAnnotations;

namespace GestionQ.Domain.Entities
{
    public class SystemSetting
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Key { get; set; } = null!;

        public string? Value { get; set; }

        public string? Description { get; set; }
    }
}
