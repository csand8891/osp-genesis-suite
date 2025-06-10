// File: RuleArchitect.ApplicationLogic/DTOs/OrderItemDto.cs
using System;

namespace RuleArchitect.Abstractions.DTOs.Order
{
    /// <summary>
    /// Data Transfer Object representing a software option line item within an order.
    /// </summary>
    public class OrderItemDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for this order item.
        /// </summary>
        public int OrderItemId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the <see cref="RuleArchitect.Entities.SoftwareOption"/> linked to this item.
        /// </summary>
        public int SoftwareOptionId { get; set; }

        /// <summary>
        /// Gets or sets the primary name of the linked <see cref="RuleArchitect.Entities.SoftwareOption"/>.
        /// </summary>
        public string SoftwareOptionName { get; set; } // Use string? if on C# 8+ and it can be null

        /// <summary>
        /// Gets or sets the primary option number display of the linked <see cref="RuleArchitect.Entities.SoftwareOption"/>.
        /// </summary>
        public string SoftwareOptionNumberDisplay { get; set; } // Use string? if on C# 8+ and it can be null

        /// <summary>
        /// Gets or sets the timestamp when this item was added to the order.
        /// </summary>
        public DateTime AddedAt { get; set; }
    }
}