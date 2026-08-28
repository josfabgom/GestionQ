using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionQ.Domain.Entities
{
    public class ProductChangeLog
    {
        [Key]
        public int Id { get; set; }
        
        public int ProductId { get; set; }
        
        [ForeignKey("ProductId")]
        public Product Product { get; set; }
        
        public string ProductName { get; set; }
        
        public string ChangeDescription { get; set; }
        
        public DateTime DateChanged { get; set; }
        
        public DateTime? DateSentToPos { get; set; }
    }
}
