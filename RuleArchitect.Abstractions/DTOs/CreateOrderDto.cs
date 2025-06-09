// In RuleArchitect.ApplicationLogic/DTOs/CreateOrderDto.cs
using RuleArchitect.Abstractions.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RuleArchitect.Abstractions.DTOs
{
    /// <summary>
    /// Data Transfer Object for creating a new order.
    /// </summary>
    public class CreateOrderDto
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
        /// Gets or sets the date the order was placed. Defaults to current UTC time if not provided by client.
        /// </summary>
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the date by which the order is required.
        /// </summary>
        public DateTime? RequiredDate { get; set; }

        /// <summary>
        /// Gets or sets initial notes for the order.
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

        /// <summary>
        /// Gets or sets a list of Software Option IDs to be included as line items in this order.
        /// </summary>
        public List<int> SoftwareOptionIds { get; set; } = new List<int>();
    }
}