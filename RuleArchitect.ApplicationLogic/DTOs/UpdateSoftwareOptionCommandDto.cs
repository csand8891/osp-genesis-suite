// File: RuleArchitect.ApplicationLogic/DTOs/UpdateSoftwareOptionCommandDto.cs
using System;
using System.Collections.Generic;

namespace RuleArchitect.ApplicationLogic.DTOs
{
    /// <summary>
    /// Data Transfer Object for updating an existing SoftwareOption and its related collections.
    /// </summary>
    public class UpdateSoftwareOptionCommandDto
    {
        /// <summary>
        /// The ID of the SoftwareOption to update. This is mandatory.
        /// </summary>
        public int SoftwareOptionId { get; set; }

        // Properties for SoftwareOption itself that can be updated
        public string PrimaryName { get; set; }
        public string AlternativeNames { get; set; } // Can be null
        public string SourceFileName { get; set; }   // Can be null
        public string PrimaryOptionNumberDisplay { get; set; } // Can be null
        public string Notes { get; set; }            // Can be null
        public int? ControlSystemId { get; set; }    // Nullable if it can be unassigned

        // Collections representing the desired state of dependent entities after the update.
        // The service layer will need to implement logic to synchronize these
        // with the existing entities (e.g., add new, remove old, update existing).

        /// <summary>
        /// The complete list of OptionNumbers that should be associated with this SoftwareOption after the update.
        /// </summary>
        public List<OptionNumberRegistryCreateDto> OptionNumbers { get; set; }

        /// <summary>
        /// The complete list of Requirements that should be associated with this SoftwareOption after the update.
        /// </summary>
        public List<RequirementCreateDto> Requirements { get; set; }

        /// <summary>
        /// The complete list of SpecificationCodes that should be associated with this SoftwareOption after the update.
        /// </summary>
        public List<SoftwareOptionSpecificationCodeCreateDto> SpecificationCodes { get; set; }

        /// <summary>
        /// The complete list of ActivationRules that should be associated with this SoftwareOption after the update.
        /// </summary>
        public List<SoftwareOptionActivationRuleCreateDto> ActivationRules { get; set; }

        // Add other lists for other dependent entities if their collections can be modified
        // during a SoftwareOption update (e.g., ParameterMappings).

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateSoftwareOptionCommandDto"/> class.
        /// </summary>
        public UpdateSoftwareOptionCommandDto()
        {
            // Initialize collections to prevent null reference exceptions if they are not provided
            OptionNumbers = new List<OptionNumberRegistryCreateDto>();
            Requirements = new List<RequirementCreateDto>();
            SpecificationCodes = new List<SoftwareOptionSpecificationCodeCreateDto>();
            ActivationRules = new List<SoftwareOptionActivationRuleCreateDto>();
        }
    }
}
