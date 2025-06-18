// In RuleArchitect.ApplicationLogic/DTOs/UpdateOrderDto.cs
using RuleArchitect.Abstractions.Enums; // For OrderStatus
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RuleArchitect.Abstractions.DTOs.Order
{
    /// <summary>
    /// Data Transfer Object for updating an existing order.
    /// </summary>
    public class UpdateOrderDto
    {
        /// <summary>
        /// Gets or sets the user-friendly order number. Must be unique.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string OrderNumber { get; set; } = null!;

        /// <summary>
        /// Gets or sets the name of the customer.
        /// </summary>
        [MaxLength(255)]
        public string? CustomerName { get; set; }

        /// <summary>
        /// Gets or sets the date the order was placed.
        /// </summary>
        public DateTime OrderDate { get; set; }

        /// <summary>
        /// Gets or sets the date by which the order is required.
        /// </summary>
        public DateTime? RequiredDate { get; set; }

        /// <summary>
        /// Gets or sets the current status of the order.
        /// </summary>
        [Required]
        public OrderStatus Status { get; set; }

        /// <summary>
        /// Gets or sets notes for the order.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Gets or sets the ID of the <see cref="Entities.ControlSystem"/> for this order.
        /// </summary>
        [Required]
        public int ControlSystemId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the specific <see cref="Entities.MachineModel"/> for this order.
        /// </summary>
        [Required]
        public int MachineModelId { get; set; }

        // Note: Updating line items (SoftwareOptionIds) might be handled by separate
        // AddSoftwareOptionToOrderAsync/RemoveSoftwareOptionFromOrderAsync methods
        // or you could include a list of SoftwareOptionIds here to completely replace existing items.
        // For simplicity, we'll assume separate methods for item management for now.
    }
}