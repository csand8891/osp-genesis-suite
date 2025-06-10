using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// In RuleArchitect.ApplicationLogic/DTOs/SoftwareOption/RequirementCreateDto.cs
namespace RuleArchitect.Abstractions.DTOs.SoftwareOption
{
    public class RequirementCreateDto
    {
        public string RequirementType { get; set; }
        public string? Condition { get; set; }
        public string? GeneralRequiredValue { get; set; }
        public int? RequiredSoftwareOptionId { get; set; }
        public int? RequiredSpecCodeDefinitionId { get; set; }
        public string? OspFileName { get; set; }
        public string? OspFileVersion { get; set; }
        public string? Notes { get; set; }
    }
}
