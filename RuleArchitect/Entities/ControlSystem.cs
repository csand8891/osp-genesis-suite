using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RuleArchitect.Entities
{
    [Table("ControlSystems")]
    public class ControlSystem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ControlSystemId { get; set; }

        [Required]
        [MaxLength(100)]
        // Removed: [Index("IX_ControlSystemName", IsUnique = true)] 
        // This index will be configured in OnModelCreating:
        // modelBuilder.Entity<ControlSystem>().HasIndex(cs => cs.Name).IsUnique().HasDatabaseName("IX_ControlSystemName");
        public string Name { get; set; } = null!; // Initialize with null forgiving for non-nullable string

        [Required]
        public int MachineTypeId { get; set; }

        [ForeignKey("MachineTypeId")]
        public virtual MachineType MachineType { get; set; } = null!; // Initialize with null forgiving

        public virtual ICollection<SoftwareOption> SoftwareOptions { get; set; }
        public virtual ICollection<SpecCodeDefinition> SpecCodeDefinitions { get; set; } // Collection of specification code definitions

        public ControlSystem()
        {
            SoftwareOptions = new HashSet<SoftwareOption>();
            SpecCodeDefinitions = new HashSet<SpecCodeDefinition>(); // Initialize the collection
        }
    }
}