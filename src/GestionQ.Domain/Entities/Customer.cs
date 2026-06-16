using System.ComponentModel.DataAnnotations;

namespace GestionQ.Domain.Entities;

public class Customer
{
    public int Id { get; set; }
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public string? Dni { get; set; }
    public string? Cuit { get; set; }
    
    [Display(Name = "Condición IVA")]
    public int? TaxConditionId { get; set; }
    public TaxCondition? TaxCondition { get; set; }

    public string? Address { get; set; }
    public string? Locality { get; set; }
    public bool IsActive { get; set; } = true;
    public string? InternalCode { get; set; }
    public string? ImageUrl { get; set; }

    public decimal Balance { get; set; }
    public decimal DiscountPercentage { get; set; } = 0;
}
