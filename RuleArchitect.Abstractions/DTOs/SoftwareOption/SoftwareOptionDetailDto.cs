// File: RuleArchitect.Abstractions/DTOs/SoftwareOptionDetailDto.cs
using System;
using System.Collections.Generic;

namespace RuleArchitect.Abstractions.DTOs.SoftwareOption
{
    /// <summary>
    /// A detailed DTO for a Software Option, including its related collections.
    /// Used for editing or viewing a single, complete Software Option.
    /// </summary>
    public class SoftwareOptionDetailDto : SoftwareOptionDto
    {
        public List<OptionNumberRegistryCreateDto> OptionNumbers { get; set; }
        public List<RequirementCreateDto> Requirements { get; set; }
        public List<SoftwareOptionSpecificationCodeCreateDto> SpecificationCodes { get; set; }
        public List<SoftwareOptionActivationRuleCreateDto> ActivationRules { get; set; }

        public SoftwareOptionDetailDto()
        {
            OptionNumbers = new List<OptionNumberRegistryCreateDto>();
            Requirements = new List<RequirementCreateDto>();
            SpecificationCodes = new List<SoftwareOptionSpecificationCodeCreateDto>();
            ActivationRules = new List<SoftwareOptionActivationRuleCreateDto>();
        }
    }
}
