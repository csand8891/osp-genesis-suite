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
        //public string RequirementType { get => _requirementType; set => SetProperty(ref _requirementType, value); }
        public string? Condition { get => _condition; set => SetProperty(ref _condition, value); }
        //public string GeneralRequiredValue { get => _generalRequiredValue; set => SetProperty(ref _generalRequiredValue, value); }
        //public int? RequiredSoftwareOptionId { get => _requiredSoftwareOptionId; set => SetProperty(ref _requiredSoftwareOptionId, value); }
        public int? RequiredSpecCodeDefinitionId
        {
            get => _requiredSpecCodeDefinitionId;
            set
            {
                // Add this line for debugging
                System.Diagnostics.Debug.WriteLine($"RequiredSpecCodeDefinitionId setter hit. New value: {value}");

                if (SetProperty(ref _requiredSpecCodeDefinitionId, value))
                {
                    // Add this line if SetProperty indicates a change occurred
                    System.Diagnostics.Debug.WriteLine($"RequiredSpecCodeDefinitionId changed to: {value}. Calling Validate().");
                    Validate(); // Trigger validation on change
                }
                else
                {
                    // Add this line if SetProperty returned false (no change)
                    System.Diagnostics.Debug.WriteLine($"RequiredSpecCodeDefinitionId setter finished, but SetProperty returned false (no change or skipped).");
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
        // Add a property that triggers validation whenever a relevant field changes
        public string RequirementType
        {
            get => _requirementType;
            set
            {
                SetProperty(ref _requirementType, value);
                Validate(); // Trigger validation on change
            }
        }

        public int? RequiredSoftwareOptionId
        {
            get => _requiredSoftwareOptionId;
            set
            {
                SetProperty(ref _requiredSoftwareOptionId, value);
                Validate(); // Trigger validation on change
            }
        }

        public string GeneralRequiredValue
        {
            get => _generalRequiredValue;
            set
            {
                SetProperty(ref _generalRequiredValue, value);
                Validate(); // Trigger validation on change
            }
        }

        public void Validate()
        {
            // Clear previous errors to re-validate the entire object
            ClearErrors();

            switch (RequirementType)
            {
                case "Software Option":
                    if (!RequiredSoftwareOptionId.HasValue || RequiredSoftwareOptionId <= 0)
                    {
                        // The property name "RequiredSoftwareOptionId" matches the ComboBox binding.
                        // WPF will automatically show the error on that control.
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

        public string RequiredValueDisplayText { get; set; }

        // --- Properties for Display (You might load these via services or get them from parent VM's lookup lists) ---
        private string? _requiredSoftwareOptionName;
        public string? RequiredSoftwareOptionName { get => _requiredSoftwareOptionName; set => SetProperty(ref _requiredSoftwareOptionName, value); }

        private string? _requiredSpecCodeName;
        public string? RequiredSpecCodeName { get => _requiredSpecCodeName; set => SetProperty(ref _requiredSpecCodeName, value); }

        // Add constructors for mapping from Entity/DTO if needed
        // public RequirementViewModel() { }
    }
}