// Current file: csand8891/osp-genesis-suite/osp-genesis-suite-development/RuleArchitect/Entities/MachineType.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RuleArchitect.Entities
{
    [Table("MachineTypes")]
    public class MachineType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MachineTypeId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!; // e.g., "Lathe", "Machining Center", "Grinder"

        public virtual ICollection<ControlSystem> ControlSystems { get; set; }
        public virtual ICollection<SpecCodeDefinition> SpecCodeDefinitions { get; set; }
        public virtual ICollection<MachineModel> MachineModels { get; set; } // <-- NEW: Collection of specific models

        public MachineType()
        {
            ControlSystems = new HashSet<ControlSystem>();
            SpecCodeDefinitions = new HashSet<SpecCodeDefinition>();
            MachineModels = new HashSet<MachineModel>(); // <-- NEW: Initialize the collection
        }
    }
}