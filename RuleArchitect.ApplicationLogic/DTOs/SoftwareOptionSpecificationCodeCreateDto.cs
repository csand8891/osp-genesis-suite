using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuleArchitect.ApplicationLogic.DTOs
{
    public class SoftwareOptionSpecificationCodeCreateDto
    {
        public int SpecCodeDefinitionId { get; set; } // Foreign key to SpecCodeDefinitions
        public int? SoftwareOptionActivationRuleId { get; set; } // Foreign key to SoftwareOptionActivationRules (nullable)
        public string SpecificInterpretation { get; set; } // Or string? if nullable types are enabled
    }
}
