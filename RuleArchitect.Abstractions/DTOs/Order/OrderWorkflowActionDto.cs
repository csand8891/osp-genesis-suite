// File: RuleArchitect.ApplicationLogic/DTOs/OrderWorkflowActionDto.cs
using System.ComponentModel.DataAnnotations;

namespace RuleArchitect.Abstractions.DTOs.Order
{
    public class OrderWorkflowActionDto
    {
        // OrderId will be passed as a separate parameter to the service method.
        // UserId will come from the authenticated user.

        [MaxLength(1000)]
        public string? Notes { get; set; } // For approval/rejection notes
    }
}