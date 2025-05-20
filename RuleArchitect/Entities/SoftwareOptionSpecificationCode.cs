using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RuleArchitect.Entities
{
    [Table("SoftwareOptionSpecificationCodes")]
    public class SoftwareOptionSpecificationCode
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SoftwareOptionSpecificationCodeId { get; set; }

        [Required]
        public int SoftwareOptionId { get; set; }

        [Required]
        public int SpecCodeDefinitionId { get; set; }

        public int? SoftwareOptionActivationRuleId { get; set; }

        public string? SpecificInterpretation { get; set; }

        [ForeignKey("SoftwareOptionId")]
        public virtual SoftwareOption SoftwareOption { get; set; } = null!;

        [ForeignKey("SpecCodeDefinitionId")]
        public virtual SpecCodeDefinition SpecCodeDefinition { get; set; } = null!; // Assuming SpecCodeDefinition is another entity class

        [ForeignKey("SoftwareOptionActivationRuleId")]
        public virtual SoftwareOptionActivationRule? SoftwareOptionActivationRule { get; set; } // Nullable as FK is nullable
    }
}