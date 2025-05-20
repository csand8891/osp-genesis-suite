using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RuleArchitect.Entities
{
    [Table("ParameterMappings")]
    public class ParameterMapping
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ParameterMappingId { get; set; }

        [MaxLength(255)]
        public string? RelatedSheetName { get; set; } // Assuming these can be nullable based on lack of [Required]

        [MaxLength(255)]
        public string? ConditionIdentifier { get; set; }

        [MaxLength(255)]
        public string? ConditionName { get; set; }

        [MaxLength(255)]
        public string? SettingContext { get; set; }

        public string ConfigurationDetailsJson { get; set; } = null!; // If this is expected to be non-null always.
                                                                      // Or string? if it can be null.

        public int? SoftwareOptionId { get; set; } // Already nullable

        [ForeignKey("SoftwareOptionId")]
        public virtual SoftwareOption? SoftwareOption { get; set; } // Navigation can be nullable if FK is nullable
    }
}