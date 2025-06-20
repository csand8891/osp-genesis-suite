using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace RuleArchitect.DesktopClient.ViewModels
{
    /// <summary>
    /// ViewModel representing a Requirement in the UI.
    /// </summary>
    public class RequirementViewModel : ValidatableViewModel
    {
        private int _originalId;
        private string _requirementType = string.Empty;
        private string? _condition;
        private string _generalRequiredValue = string.Empty;
        private int? _requiredSoftwareOptionId;
        private int? _requiredSpecCodeDefinitionId;
        private string? _ospFileName;
        private string? _ospFileVersion;
        private string? _notes;

        // ** UPDATED: This property now supports change notification **
        private string _requiredValueDisplayText;
        public string RequiredValueDisplayText
        {
            get => _requiredValueDisplayText;
            set => SetProperty(ref _requiredValueDisplayText, value);
        }

        // --- Properties for UI Binding ---
        public int OriginalId { get => _originalId; set => SetProperty(ref _originalId, value); }
        public string? Condition { get => _condition; set => SetProperty(ref _condition, value); }
        public int? RequiredSpecCodeDefinitionId
        {
            get => _requiredSpecCodeDefinitionId;
            set
            {
                if (SetProperty(ref _requiredSpecCodeDefinitionId, value))
                {
                    Validate();
                }
            }
        }
        public string? OspFileName { get => _ospFileName; set => SetProperty(ref _ospFileName, value); }
        public string? OspFileVersion { get => _ospFileVersion; set => SetProperty(ref _ospFileVersion, value); }
        public string? Notes { get => _notes; set => SetProperty(ref _notes, value); }

        public static List<string> AvailableRequirementTypes { get; } = new List<string>
        {
            "Software Option", "Spec Code", "OSP File Version", "General Text"
        };

        public static List<string> AvailableConditions { get; } = new List<string>
        {
            "requires", "excludes", "minimumversion"
        };

        public string RequirementType
        {
            get => _requirementType;
            set
            {
                SetProperty(ref _requirementType, value);
                Validate();
            }
        }

        public int? RequiredSoftwareOptionId
        {
            get => _requiredSoftwareOptionId;
            set
            {
                SetProperty(ref _requiredSoftwareOptionId, value);
                Validate();
            }
        }

        public string GeneralRequiredValue
        {
            get => _generalRequiredValue;
            set
            {
                SetProperty(ref _generalRequiredValue, value);
                Validate();
            }
        }

        public void Validate()
        {
            ClearErrors();

            switch (RequirementType)
            {
                case "Software Option":
                    if (!RequiredSoftwareOptionId.HasValue || RequiredSoftwareOptionId <= 0)
                    {
                        AddError(nameof(RequiredSoftwareOptionId), "A required software option must be selected.");
                    }
                    break;

                case "Spec Code":
                    if (!RequiredSpecCodeDefinitionId.HasValue || RequiredSpecCodeDefinitionId <= 0)
                    {
                        AddError(nameof(RequiredSpecCodeDefinitionId), "A required spec code must be selected.");
                    }
                    break;

                case "General Text":
                    if (string.IsNullOrWhiteSpace(GeneralRequiredValue))
                    {
                        AddError(nameof(GeneralRequiredValue), "A value must be provided.");
                    }
                    break;
            }
        }

        private string? _requiredSoftwareOptionName;
        public string? RequiredSoftwareOptionName { get => _requiredSoftwareOptionName; set => SetProperty(ref _requiredSoftwareOptionName, value); }

        private string? _requiredSpecCodeName;
        public string? RequiredSpecCodeName { get => _requiredSpecCodeName; set => SetProperty(ref _requiredSpecCodeName, value); }
    }
}
