namespace GestionQ.Domain.Entities
{
    public class PromotionRuleProduct
    {
        public int PromotionRuleId { get; set; }
        public PromotionRule? PromotionRule { get; set; }
        
        public int ProductId { get; set; }
        public Product? Product { get; set; }
    }
}
