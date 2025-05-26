// File: RuleArchitect.ApplicationLogic/DTOs/OrderSummaryDto.cs
using RuleArchitect.Entities; // For OrderStatus enum
using System;

namespace RuleArchitect.ApplicationLogic.DTOs
{
    public class OrderSummaryDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public OrderStatus Status { get; set; }
        public string StatusText => Status.ToString(); // For easy binding
        public DateTime OrderDate { get; set; }
        public DateTime? RequiredDate { get; set; }
        public string? CreatedByUserName { get; set; } // For display
    }
}