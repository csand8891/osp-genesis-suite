using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RuleArchitect.Entities
{
    /// <summary>
    /// Represents a specific machine model, such as "LB3000" or "MB-4000H".
    /// Each specific model belongs to a more general <see cref="MachineType"/>.
    /// </summary>
    [Table("MachineModels")]
    public class MachineModel
    {
        /// <summary>
        /// Gets or sets the unique identifier for the machine model.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MachineModelId { get; set; }

        /// <summary>
        /// Gets or sets the name of the specific machine model (e.g., "LB3000", "MB-4000H").
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Gets or sets the foreign key to the <see cref="MachineType"/> this model belongs to.
        /// </summary>
        [Required]
        public int MachineTypeId { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the generic <see cref="MachineType"/> 
        /// (e.g., "Lathe", "Machining Center") this model is categorized under.
        /// </summary>
        [ForeignKey("MachineTypeId")]
        public virtual MachineType MachineType { get; set; } = null!;

        // /// <summary>
        // /// Gets or sets a collection of <see cref="Order"/> entities that specify this machine model.
        // /// This property is optional and would be used if you need to navigate from a MachineModel to all its associated Orders.
        // /// </summary>
        // public virtual ICollection<Order> Orders { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MachineModel"/> class.
        /// </summary>
        public MachineModel()
        {
            // if (Orders == null) // Initialize only if you uncomment the Orders collection
            // {
            //     Orders = new HashSet<Order>();
            // }
        }
    }
}