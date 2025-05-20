using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RuleArchitect.Entities
{
    /// <summary>
    /// Stores the master definitions of specification codes (No. and Bit), specific to a MachineType.
    /// </summary>
    [Table("SpecCodeDefinitions")]
    public class SpecCodeDefinition
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SpecCodeDefinitionId { get; set; }

        [Required]
        [MaxLength(50)]
        // Removed: [Index("IX_SpecCodeNoBitMachineType", 1, IsUnique = true)]
        // This composite index will be configured in OnModelCreating:
        // modelBuilder.Entity<SpecCodeDefinition>()
        //     .HasIndex(scd => new { scd.SpecCodeNo, scd.SpecCodeBit, scd.MachineTypeId })
        //     .IsUnique()
        //     .HasName("IX_SpecCodeNoBitMachineType");
        public string SpecCodeNo { get; set; } = null!; // Initialize with null forgiving

        [Required]
        [MaxLength(50)]
        // Removed: [Index("IX_SpecCodeNoBitMachineType", 2, IsUnique = true)]
        public string SpecCodeBit { get; set; } = null!; // Initialize with null forgiving

        [MaxLength(255)]
        public string? Description { get; set; } // Assuming Description can be nullable

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = null!; // Initialize with null forgiving

        [Required]
        // Removed: [Index("IX_SpecCodeNoBitMachineType", 3, IsUnique = true)]
        public int MachineTypeId { get; set; }

        // Foreign Key relationship
        [ForeignKey("MachineTypeId")]
        public virtual MachineType MachineType { get; set; } = null!; // Initialize with null forgiving

        // Navigation property
        public virtual ICollection<SoftwareOptionSpecificationCode> SoftwareOptionSpecificationCodes { get; set; }
        public virtual ICollection<Requirement> Requirements { get; set; }

        public SpecCodeDefinition()
        {
            SoftwareOptionSpecificationCodes = new HashSet<SoftwareOptionSpecificationCode>();
            Requirements = new HashSet<Requirement>();
        }
    }
}