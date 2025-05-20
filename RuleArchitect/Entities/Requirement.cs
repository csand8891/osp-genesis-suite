using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RuleArchitect.Entities
{
    [Table("Requirements")]
    public class Requirement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequirementId { get; set; }

        [Required]
        public int SoftwareOptionId { get; set; }

        [Required]
        [MaxLength(100)]
        public string RequirementType { get; set; } = null!;

        [MaxLength(100)]
        public string? Condition { get; set; }

        public string GeneralRequiredValue { get; set; } = null!; // Assuming non-null, else string?

        public int? RequiredSoftwareOptionId { get; set; }

        public int? RequiredSpecCodeDefinitionId { get; set; }

        [MaxLength(255)]
        public string? OspFileName { get; set; }

        [MaxLength(50)]
        public string? OspFileVersion { get; set; }

        public string? Notes { get; set; }

        [ForeignKey("SoftwareOptionId")]
        public virtual SoftwareOption SoftwareOption { get; set; } = null!;

        [ForeignKey("RequiredSoftwareOptionId")]
        public virtual SoftwareOption? RequiredSoftwareOption { get; set; } // Nullable as FK is nullable

        [ForeignKey("RequiredSpecCodeDefinitionId")]
        public virtual SpecCodeDefinition? RequiredSpecCodeDefinition { get; set; } // Nullable as FK is nullable
    }
}