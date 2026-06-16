using System;
using System.Collections.Generic;

namespace GestionQ.Domain.Entities
{
    public class PromotionRule
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public PromotionType Type { get; set; }
        public decimal Value { get; set; }
        
        public int? BuyQuantity { get; set; }
        public int? PayQuantity { get; set; }
        
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsStackable { get; set; } = false;
        
        public ICollection<PromotionRuleProduct> Products { get; set; } = new List<PromotionRuleProduct>();
    }

    public enum PromotionType
    {
        Percentage,
        FixedAmount,
        XForY,
        Volume
    }
}
