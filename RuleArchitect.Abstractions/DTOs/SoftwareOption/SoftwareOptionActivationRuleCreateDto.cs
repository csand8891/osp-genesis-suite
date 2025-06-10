using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuleArchitect.Abstractions.DTOs.SoftwareOption
{
    public class SoftwareOptionActivationRuleCreateDto
    {
        public string RuleName { get; set; } // Or string?
        public string ActivationSetting { get; set; }
        public string Notes { get; set; } // Or string?
    }
}
