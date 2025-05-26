using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuleArchitect.ApplicationLogic.DTOs
{
    /// <summary>
    /// Data Transfer Object for specifying filter criteria when retrieving a list of orders.
    /// All filter properties are optional.
    /// </summary>
    public class OrderFilterDto
    {
        /// <summary>
        /// Gets or sets the specific order status to filter by.
        /// </summary>
        public Entities.OrderStatus? Status { get; set; } // Assuming OrderStatus is in RuleArchitect.Entities

        /// <summary>
        /// Gets or sets a part of the customer name to filter by (case-insensitive contains).
        /// </summary>
        public string? CustomerName { get; set; }

        /// <summary>
        /// Gets or sets a part of the order number to filter by (case-insensitive contains).
        /// </summary>
        public string? OrderNumber { get; set; }

        /// <summary>
        /// Gets or sets the start date for the order creation date range filter.
        /// </summary>
        public DateTime? OrderDateFrom { get; set; }

        /// <summary>
        /// Gets or sets the end date for the order creation date range filter.
        /// </summary>
        public DateTime? OrderDateTo { get; set; }

        /// <summary>
        /// Gets or sets the ID of the Control System to filter orders by.
        /// </summary>
        public int? ControlSystemId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the Machine Model to filter orders by.
        /// </summary>
        public int? MachineModelId { get; set; }

        // Add other potential filter properties as needed, e.g.:
        // public int? CreatedByUserId { get; set; }
        // public int? PageNumber { get; set; } = 1;
        // public int? PageSize { get; set; } = 20;
    }
}
