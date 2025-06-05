using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuleArchitect.Abstractions.DTOs
{
    public class CreateSoftwareOptionCommandDto
    {
        public string PrimaryName { get; set; }
        public string? AlternativeNames { get; set; }
        public string? SourceFileName { get; set; }
        public string? PrimaryOptionNumberDisplay { get; set; }
        public string? Notes { get; set; }
        public int? ControlSystemId { get; set; }

        // Collections for dependent entities
        public List<OptionNumberRegistryCreateDto> OptionNumbers { get; set; }
        public List<RequirementCreateDto> Requirements { get; set; }
        public List<SoftwareOptionSpecificationCodeCreateDto> SpecificationCodes { get; set; }
        public List<SoftwareOptionActivationRuleCreateDto> ActivationRules { get; set; }



        public CreateSoftwareOptionCommandDto()
        {
            OptionNumbers = new List<OptionNumberRegistryCreateDto>();
            Requirements = new List<RequirementCreateDto>();
            SpecificationCodes = new List<SoftwareOptionSpecificationCodeCreateDto>();
            ActivationRules = new List<SoftwareOptionActivationRuleCreateDto>();
        }
    }
}
