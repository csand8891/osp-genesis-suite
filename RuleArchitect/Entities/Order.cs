using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RuleArchitect.Abstractions.Enums;
using GenesisSentry.Entities; // For UserEntity
using RuleArchitect.Entities; // For ControlSystem, MachineModel, OrderItem, OrderStatus

namespace RuleArchitect.Entities
{
    /// <summary>
    /// Represents a customer or production order within the system.
    /// An order specifies the required <see cref="ControlSystem"/>, <see cref="MachineModel"/>,
    /// and a collection of <see cref="OrderItem"/>s which detail the software options.
    /// It also tracks various stages of approval and completion.
    /// </summary>
    [Table("Orders")]
    public class Order
    {
        /// <summary>
        /// Gets or sets the unique identifier for the order.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderId { get; set; }

        /// <summary>
        /// Gets or sets the unique, user-friendly order number.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string OrderNumber { get; set; } = null!;

        /// <summary>
        /// Gets or sets the name of the customer associated with the order.
        /// </summary>
        [MaxLength(255)]
        public string? CustomerName { get; set; }

        /// <summary>
        /// Gets or sets the date the order was created or placed.
        /// </summary>
        [Required]
        public DateTime OrderDate { get; set; }

        /// <summary>
        /// Gets or sets the date by which the order is required or due.
        /// </summary>
        public DateTime? RequiredDate { get; set; }

        /// <summary>
        /// Gets or sets the current status of the order.
        /// </summary>
        [Required]
        public OrderStatus Status { get; set; }

        /// <summary>
        /// Gets or sets general notes or comments related to the order.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Gets or sets the foreign key for the <see cref="ControlSystem"/> (e.g., "P300LA") associated with this order.
        /// </summary>
        [Required]
        public int ControlSystemId { get; set; }

        /// <summary>
        /// Gets or sets the foreign key for the specific <see cref="MachineModel"/> (e.g., "LB3000") associated with this order.
        /// </summary>
        [Required]
        public int MachineModelId { get; set; }

        /// <summary>
        /// Gets or sets the foreign key for the <see cref="UserEntity"/> who created the order.
        /// </summary>
        [Required]
        public int CreatedByUserId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the order was created.
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the foreign key for the <see cref="UserEntity"/> who reviewed the order.
        /// </summary>
        public int? OrderReviewerUserId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the order was reviewed.
        /// </summary>
        public DateTime? OrderReviewedAt { get; set; }

        /// <summary>
        /// Gets or sets any notes provided by the order reviewer.
        /// </summary>
        public string? OrderReviewNotes { get; set; }

        /// <summary>
        /// Gets or sets the foreign key for the <see cref="UserEntity"/> (production technician) who completed the production.
        /// </summary>
        public int? ProductionTechUserId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the production for the order was completed.
        /// </summary>
        public DateTime? ProductionCompletedAt { get; set; }

        /// <summary>
        /// Gets or sets any notes provided by the production technician.
        /// </summary>
        public string? ProductionNotes { get; set; }

        /// <summary>
        /// Gets or sets the foreign key for the <see cref="UserEntity"/> who reviewed the software aspects of the order.
        /// </summary>
        public int? SoftwareReviewerUserId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the software aspects of the order were reviewed.
        /// </summary>
        public DateTime? SoftwareReviewedAt { get; set; }

        /// <summary>
        /// Gets or sets any notes provided by the software reviewer.
        /// </summary>
        public string? SoftwareReviewNotes { get; set; }

        /// <summary>
        /// Gets or sets the foreign key for the <see cref="UserEntity"/> who last modified the order.
        /// </summary>
        public int? LastModifiedByUserId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the order was last modified.
        /// </summary>
        public DateTime? LastModifiedAt { get; set; }

        // --- Navigation Properties ---

        /// <summary>
        /// Gets or sets the navigation property to the <see cref="ControlSystem"/> associated with this order.
        /// </summary>
        [ForeignKey("ControlSystemId")]
        public virtual ControlSystem ControlSystem { get; set; } = null!;

        /// <summary>
        /// Gets or sets the navigation property to the specific <see cref="MachineModel"/> associated with this order.
        /// </summary>
        [ForeignKey("MachineModelId")]
        public virtual MachineModel MachineModel { get; set; } = null!;

        /// <summary>
        /// Gets or sets the navigation property to the <see cref="UserEntity"/> who created this order.
        /// </summary>
        [ForeignKey("CreatedByUserId")]
        public virtual UserEntity CreatedByUser { get; set; } = null!;

        /// <summary>
        /// Gets or sets the navigation property to the <see cref="UserEntity"/> who reviewed this order.
        /// </summary>
        [ForeignKey("OrderReviewerUserId")]
        public virtual UserEntity? OrderReviewerUser { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the <see cref="UserEntity"/> (production technician) associated with this order's production.
        /// </summary>
        [ForeignKey("ProductionTechUserId")]
        public virtual UserEntity? ProductionTechUser { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the <see cref="UserEntity"/> who performed the software review for this order.
        /// </summary>
        [ForeignKey("SoftwareReviewerUserId")]
        public virtual UserEntity? SoftwareReviewerUser { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the <see cref="UserEntity"/> who last modified this order.
        /// </summary>
        [ForeignKey("LastModifiedByUserId")]
        public virtual UserEntity? LastModifiedByUser { get; set; }

        /// <summary>
        /// Gets or sets the collection of <see cref="OrderItem"/>s associated with this order.
        /// </summary>
        public virtual ICollection<OrderItem> OrderItems { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Order"/> class,
        /// setting default values for <see cref="OrderItems"/>, <see cref="CreatedAt"/>,
        /// <see cref="Status"/>, and <see cref="OrderDate"/>.
        /// </summary>
        public Order()
        {
            OrderItems = new HashSet<OrderItem>();
            CreatedAt = DateTime.UtcNow;
            Status = OrderStatus.Draft; // Assumes OrderStatus enum is defined elsewhere in RuleArchitect.Entities
            OrderDate = DateTime.UtcNow;
        }
    }
}