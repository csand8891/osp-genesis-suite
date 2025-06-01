// RuleArchitect.DesktopClient/ViewModels/SpecCodeViewModel.cs
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RuleArchitect.DesktopClient.ViewModels
{
    public class SpecCodeViewModel : BaseViewModel
    {
        private int _originalId; // SoftwareOptionSpecificationCodeId
        private int _specCodeDefinitionId;
        private int? _softwareOptionActivationRuleId;
        private string? _specificInterpretation;

        public int OriginalId { get => _originalId; set => SetProperty(ref _originalId, value); }

        // This ID will be bound to a ComboBox or a lookup
        public int SpecCodeDefinitionId
        {
            get => _specCodeDefinitionId;
            set => SetProperty(ref _specCodeDefinitionId, value,
                onChanged: () => {
                    // Optionally update SpecCodeDisplayName if you fetch/resolve it here
                    OnPropertyChanged(nameof(SpecCodeDisplayName)); // If display name depends on it
                    ItemChangedCallback?.Invoke(); // Notify parent if item changed
                });
        }

        // This ID might also be a ComboBox or lookup
        public int? SoftwareOptionActivationRuleId
        {
            get => _softwareOptionActivationRuleId;
            set => SetProperty(ref _softwareOptionActivationRuleId, value,
                onChanged: () => {
                    // Optionally update ActivationRuleName
                    OnPropertyChanged(nameof(ActivationRuleName)); // If display name depends on it
                    ItemChangedCallback?.Invoke();
                });
        }

        public string? SpecificInterpretation
        {
            get => _specificInterpretation;
            set => SetProperty(ref _specificInterpretation, value,
                onChanged: () => ItemChangedCallback?.Invoke());
        }

        // --- Properties for Display (populated by EditSoftwareOptionViewModel or a service) ---
        private string? _specCodeDisplayName; // e.g., "SpecCodeNo - SpecCodeBit - Description"
        public string? SpecCodeDisplayName
        {
            get => _specCodeDisplayName;
            set => SetProperty(ref _specCodeDisplayName, value); // Typically set when SpecCodeDefinitionId changes or on load
        }

        private string? _activationRuleName;
        public string? ActivationRuleName
        {
            get => _activationRuleName;
            set => SetProperty(ref _activationRuleName, value); // Typically set when SoftwareOptionActivationRuleId changes or on load
        }

        // TODO: Consider adding a parameterless constructor if needed for direct instantiation in lists,
        // or ensure all necessary data is passed if creating it with parameters.
        public SpecCodeViewModel() { }
    }
}