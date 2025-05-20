using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RuleArchitect.Entities
{
    [Table("SoftwareOptionActivationRules")]
    public class SoftwareOptionActivationRule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SoftwareOptionActivationRuleId { get; set; }

        [Required]
        public int SoftwareOptionId { get; set; }

        [MaxLength(255)]
        public string? RuleName { get; set; }

        [Required]
        [MaxLength(255)]
        public string ActivationSetting { get; set; } = null!;

        public string? Notes { get; set; }

        [ForeignKey("SoftwareOptionId")]
        public virtual SoftwareOption SoftwareOption { get; set; } = null!;

        public virtual ICollection<SoftwareOptionSpecificationCode> SoftwareOptionSpecificationCodes { get; set; }

        public SoftwareOptionActivationRule()
        {
            SoftwareOptionSpecificationCodes = new HashSet<SoftwareOptionSpecificationCode>();
        }
    }
}