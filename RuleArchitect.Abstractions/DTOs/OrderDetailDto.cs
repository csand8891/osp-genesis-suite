// In RuleArchitect.ApplicationLogic/DTOs/OrderDetailDto.cs
using RuleArchitect.Entities; // For OrderStatus
using System;
using System.Collections.Generic;
using RuleArchitect.ApplicationLogic.DTOs;

namespace RuleArchitect.ApplicationLogic.DTOs
{
    /// <summary>
    /// Data Transfer Object representing the detailed view of an order.
    /// </summary>
    public class OrderDetailDto
    {
        /// <summary>Gets or sets the unique identifier for the order.</summary>
        public int OrderId { get; set; }
        /// <summary>Gets or sets the user-friendly order number.</summary>
        public string OrderNumber { get; set; } = null!;
        /// <summary>Gets or sets the name of the customer.</summary>
        public string? CustomerName { get; set; }
        /// <summary>Gets or sets the date the order was placed.</summary>
        public DateTime OrderDate { get; set; }
        /// <summary>Gets or sets the date by which the order is required.</summary>
        public DateTime? RequiredDate { get; set; }
        /// <summary>Gets or sets the current status of the order.</summary>
        public OrderStatus Status { get; set; }
        /// <summary>Gets or sets general notes for the order.</summary>
        public string? Notes { get; set; }

        /// <summary>Gets or sets the ID of the associated control system.</summary>
        public int ControlSystemId { get; set; }
        /// <summary>Gets or sets the name of the associated control system (e.g., "P300LA").</summary>
        public string? ControlSystemName { get; set; }

        /// <summary>Gets or sets the ID of the associated machine model.</summary>
        public int MachineModelId { get; set; }
        /// <summary>Gets or sets the name of the associated machine model (e.g., "LB3000").</summary>
        public string? MachineModelName { get; set; }
        /// <summary>Gets or sets the name of the generic machine type (e.g., "Lathe").</summary>
        public string? MachineTypeName { get; set; }

        /// <summary>Gets or sets the ID of the user who created the order.</summary>
        public int CreatedByUserId { get; set; }
        /// <summary>Gets or sets the username of the user who created the order.</summary>
        public string? CreatedByUserName { get; set; }
        /// <summary>Gets or sets the timestamp when the order was created.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Gets or sets the ID of the user who reviewed the order.</summary>
        public int? OrderReviewerUserId { get; set; }
        /// <summary>Gets or sets the username of the user who reviewed the order.</summary>
        public string? OrderReviewerUserName { get; set; }
        /// <summary>Gets or sets the timestamp when the order was reviewed.</summary>
        public DateTime? OrderReviewedAt { get; set; }
        /// <summary>Gets or sets notes from the order reviewer.</summary>
        public string? OrderReviewNotes { get; set; }

        /// <summary>Gets or sets the ID of the production technician.</summary>
        public int? ProductionTechUserId { get; set; }
        /// <summary>Gets or sets the username of the production technician.</summary>
        public string? ProductionTechUserName { get; set; }
        /// <summary>Gets or sets the timestamp when production was completed.</summary>
        public DateTime? ProductionCompletedAt { get; set; }
        /// <summary>Gets or sets notes from production.</summary>
        public string? ProductionNotes { get; set; }

        /// <summary>Gets or sets the ID of the software reviewer.</summary>
        public int? SoftwareReviewerUserId { get; set; }
        /// <summary>Gets or sets the username of the software reviewer.</summary>
        public string? SoftwareReviewerUserName { get; set; }
        /// <summary>Gets or sets the timestamp when software was reviewed.</summary>
        public DateTime? SoftwareReviewedAt { get; set; }
        /// <summary>Gets or sets notes from the software reviewer.</summary>
        public string? SoftwareReviewNotes { get; set; }

        /// <summary>Gets or sets the ID of the user who last modified the order.</summary>
        public int? LastModifiedByUserId { get; set; }
        /// <summary>Gets or sets the username of the user who last modified the order.</summary>
        public string? LastModifiedByUserName { get; set; }
        /// <summary>Gets or sets the timestamp of the last modification.</summary>
        public DateTime? LastModifiedAt { get; set; }

        /// <summary>
        /// Gets or sets the list of software option line items for this order.
        /// </summary>
        public List<OrderItemDto> OrderItems { get; set; } = new List<OrderItemDto>();
    }
}