using System;
using System.ComponentModel.DataAnnotations;

namespace GestionQ.Domain.DTOs
{
    public class AddMovementDto
    {
        public int CashRegisterId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}
