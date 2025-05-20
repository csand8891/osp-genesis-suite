using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RuleArchitect.Entities
{
    [Table("SoftwareOptions")]
    public class SoftwareOption
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SoftwareOptionId { get; set; }

        [Required]
        [MaxLength(255)]
        public string PrimaryName { get; set; } = null!;

        [MaxLength(500)]
        public string? AlternativeNames { get; set; }

        [MaxLength(255)]
        public string? SourceFileName { get; set; }

        [MaxLength(100)]
        public string? PrimaryOptionNumberDisplay { get; set; }

        public string? Notes { get; set; }

        [MaxLength(100)]
        public string? CheckedBy { get; set; }

        public DateTime? CheckedDate { get; set; }

        public int? ControlSystemId { get; set; }

        [Required]
        public int Version { get; set; } = 1;

        [Required]
        public DateTime LastModifiedDate { get; set; }

        [MaxLength(100)]
        public string? LastModifiedBy { get; set; }

        [ForeignKey("ControlSystemId")]
        public virtual ControlSystem? ControlSystem { get; set; } // Nullable as FK is nullable

        public virtual ICollection<OptionNumberRegistry> OptionNumberRegistries { get; set; }
        public virtual ICollection<SoftwareOptionActivationRule> SoftwareOptionActivationRules { get; set; }
        public virtual ICollection<SoftwareOptionSpecificationCode> SoftwareOptionSpecificationCodes { get; set; }
        public virtual ICollection<Requirement> Requirements { get; set; } // Requirements where this SO is the primary one
        public virtual ICollection<Requirement> RequiredByOptions { get; set; } // Requirements where this SO is the 'RequiredSoftwareOption'
        public virtual ICollection<ParameterMapping> ParameterMappings { get; set; }
        public virtual ICollection<SoftwareOptionHistory> Histories { get; set; }

        public SoftwareOption()
        {
            OptionNumberRegistries = new HashSet<OptionNumberRegistry>();
            SoftwareOptionActivationRules = new HashSet<SoftwareOptionActivationRule>();
            SoftwareOptionSpecificationCodes = new HashSet<SoftwareOptionSpecificationCode>();
            Requirements = new HashSet<Requirement>();
            RequiredByOptions = new HashSet<Requirement>();
            ParameterMappings = new HashSet<ParameterMapping>();
            Histories = new HashSet<SoftwareOptionHistory>();
            LastModifiedDate = DateTime.UtcNow;
        }
    }
}