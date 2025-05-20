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
        // Removed: [Index("IX_MachineTypeName", IsUnique = true)]
        // This index will be configured in OnModelCreating:
        // modelBuilder.Entity<MachineType>().HasIndex(mt => mt.Name).IsUnique().HasDatabaseName("IX_MachineTypeName");
        public string Name { get; set; } = null!; // Initialize with null forgiving

        public virtual ICollection<ControlSystem> ControlSystems { get; set; }
        public virtual ICollection<SpecCodeDefinition> SpecCodeDefinitions { get; set; }

        public MachineType()
        {
            ControlSystems = new HashSet<ControlSystem>();
            SpecCodeDefinitions = new HashSet<SpecCodeDefinition>();
        }
    }
}