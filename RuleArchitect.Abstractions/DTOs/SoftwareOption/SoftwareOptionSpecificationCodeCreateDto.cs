using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuleArchitect.Abstractions.DTOs.SoftwareOption
{
    public class SoftwareOptionSpecificationCodeCreateDto
    {

        [Required]
        [MaxLength(50)]
        public string Category { get; set; }

        [Required]
        [MaxLength(50)]
        public string SpecCodeNo { get; set; }

        [Required]
        [MaxLength(50)]
        public string SpecCodeBit { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }
        //public int SpecCodeDefinitionId { get; set; } // Foreign key to SpecCodeDefinitions
        public int? SoftwareOptionActivationRuleId { get; set; } // Foreign key to SoftwareOptionActivationRules (nullable)
        public string? SpecificInterpretation { get; set; } // Or string? if nullable types are enabled
    }
}
