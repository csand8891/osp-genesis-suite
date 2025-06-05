using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace RuleArchitect.DesktopClient.ViewModels
{
    /// <summary>
    /// ViewModel representing a Requirement in the UI.
    /// </summary>
    public class RequirementViewModel : BaseViewModel
    {
        private int _originalId; // Or a Guid if your actual IDs are Guids
        private string _requirementType = string.Empty;
        private string? _condition;
        private string _generalRequiredValue = string.Empty;
        private int? _requiredSoftwareOptionId;
        private int? _requiredSpecCodeDefinitionId;
        private string? _ospFileName;
        private string? _ospFileVersion;
        private string? _notes;

        // --- Properties for UI Binding ---
        public int OriginalId { get => _originalId; set => SetProperty(ref _originalId, value); }
        public string RequirementType { get => _requirementType; set => SetProperty(ref _requirementType, value); }
        public string? Condition { get => _condition; set => SetProperty(ref _condition, value); }
        public string GeneralRequiredValue { get => _generalRequiredValue; set => SetProperty(ref _generalRequiredValue, value); }
        public int? RequiredSoftwareOptionId { get => _requiredSoftwareOptionId; set => SetProperty(ref _requiredSoftwareOptionId, value); }
        public int? RequiredSpecCodeDefinitionId { get => _requiredSpecCodeDefinitionId; set => SetProperty(ref _requiredSpecCodeDefinitionId, value); }
        public string? OspFileName { get => _ospFileName; set => SetProperty(ref _ospFileName, value); }
        public string? OspFileVersion { get => _ospFileVersion; set => SetProperty(ref _ospFileVersion, value); }
        public string? Notes { get => _notes; set => SetProperty(ref _notes, value); }

        public static List<string> AvailableRequirementTypes { get; } = new List<string>
        {
            "Software Option", "Spec Code", "OSP File Version", "General Text"
        };

        // --- Properties for Display (You might load these via services or get them from parent VM's lookup lists) ---
        private string? _requiredSoftwareOptionName;
        public string? RequiredSoftwareOptionName { get => _requiredSoftwareOptionName; set => SetProperty(ref _requiredSoftwareOptionName, value); }

        private string? _requiredSpecCodeName;
        public string? RequiredSpecCodeName { get => _requiredSpecCodeName; set => SetProperty(ref _requiredSpecCodeName, value); }

        // Add constructors for mapping from Entity/DTO if needed
        // public RequirementViewModel() { }
    }
}