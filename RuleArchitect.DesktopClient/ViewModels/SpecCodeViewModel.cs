using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RuleArchitect.DesktopClient.ViewModels
{
    /// <summary>
    /// ViewModel representing a SoftwareOptionSpecificationCode in the UI.
    /// </summary>
    public class SpecCodeViewModel : BaseViewModel
    {
        private int _originalId;
        private int _specCodeDefinitionId;
        private int? _softwareOptionActivationRuleId;
        private string? _specificInterpretation;

        // --- Properties for UI Binding ---
        public int OriginalId { get => _originalId; set => SetProperty(ref _originalId, value); }
        public int SpecCodeDefinitionId { get => _specCodeDefinitionId; set => SetProperty(ref _specCodeDefinitionId, value); }
        public int? SoftwareOptionActivationRuleId { get => _softwareOptionActivationRuleId; set => SetProperty(ref _softwareOptionActivationRuleId, value); }
        public string? SpecificInterpretation { get => _specificInterpretation; set => SetProperty(ref _specificInterpretation, value); }

        // --- Properties for Display (You might load these via services or get them from parent VM's lookup lists) ---
        private string? _specCodeDisplayName; // e.g., "SpecCodeNo - SpecCodeBit - Description"
        public string? SpecCodeDisplayName { get => _specCodeDisplayName; set => SetProperty(ref _specCodeDisplayName, value); }

        private string? _activationRuleName;
        public string? ActivationRuleName { get => _activationRuleName; set => SetProperty(ref _activationRuleName, value); }

        // Add constructors for mapping from Entity/DTO if needed
        // public SpecCodeViewModel() { }
    }
}