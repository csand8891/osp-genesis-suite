// File: RuleArchitect.ApplicationLogic/DTOs/CreateOrderItemDto.cs
using System.ComponentModel.DataAnnotations;

namespace RuleArchitect.ApplicationLogic.DTOs
{
    public class CreateOrderItemDto
    {
        [Required]
        public int SoftwareOptionId { get; set; }
        // You might add Quantity here if applicable
        // public int Quantity { get; set; } = 1;
    }
}