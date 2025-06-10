// File: RuleArchitect.ApplicationLogic/DTOs/UpdateSoftwareOptionCommandDto.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // For potential validation attributes

namespace RuleArchitect.Abstractions.DTOs.SoftwareOption
{
    /// <summary>
    /// Data Transfer Object for updating an existing SoftwareOption.
    /// Allows for updating scalar properties and optionally replacing related collections.
    /// </summary>
    public class UpdateSoftwareOptionCommandDto
    {
        /// <summary>
        /// The ID of the SoftwareOption to update. This is mandatory.
        /// </summary>
        [Required]
        public int SoftwareOptionId { get; set; }

        // --- Scalar Properties ---
        // Include all scalar properties from SoftwareOption that can be modified.
        // The service layer will compare these to the existing entity to detect changes.

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


        // --- Related Collections (Nullable) ---
        // If a list is provided (not null), its contents will REPLACE the existing collection.
        // If a list is null, the existing collection will NOT be changed.

        /// <summary>
        /// If provided, replaces the existing Option Numbers. If null, existing ones remain.
        /// </summary>
        public List<OptionNumberRegistryCreateDto>? OptionNumbers { get; set; }

        /// <summary>
        /// If provided, replaces the existing Requirements. If null, existing ones remain.
        /// </summary>
        public List<RequirementCreateDto>? Requirements { get; set; }

        /// <summary>
        /// If provided, replaces the existing Specification Codes. If null, existing ones remain.
        /// </summary>
        public List<SoftwareOptionSpecificationCodeCreateDto>? SpecificationCodes { get; set; }

        /// <summary>
        /// If provided, replaces the existing Activation Rules. If null, existing ones remain.
        /// </summary>
        public List<SoftwareOptionActivationRuleCreateDto>? ActivationRules { get; set; }

        // Add other collections like ParameterMappings if they should also be updatable.
        // public List<ParameterMappingDto>? ParameterMappings { get; set; }

        /// <summary>
        /// Initializes a new instance of the UpdateSoftwareOptionCommandDto.
        /// By default, all collections are null, meaning an update using this
        /// default object would only attempt to update scalar properties.
        /// </summary>
        public UpdateSoftwareOptionCommandDto()
        {
            // By default, collections are null to indicate "no change intended"
            OptionNumbers = null;
            Requirements = null;
            SpecificationCodes = null;
            ActivationRules = null;
        }
    }
}