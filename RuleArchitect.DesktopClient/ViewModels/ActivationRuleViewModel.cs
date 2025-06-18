using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RuleArchitect.DesktopClient.ViewModels
{
    /// <summary>
    /// ViewModel representing a SoftwareOptionActivationRule in the UI.
    /// </summary>
    public class ActivationRuleViewModel : BaseViewModel
    {
        private int _originalId;
        private string? _ruleName;
        private string _activationSetting = string.Empty;
        private string? _notes;

        // --- Properties for UI Binding ---
        public int TempId { get; set; }
        public int OriginalId { get => _originalId; set => SetProperty(ref _originalId, value); }
        public string? RuleName { get => _ruleName; set => SetProperty(ref _ruleName, value); }
        public string ActivationSetting { get => _activationSetting; set => SetProperty(ref _activationSetting, value); }
        public string? Notes { get => _notes; set => SetProperty(ref _notes, value); }

        // Add constructors for mapping from Entity/DTO if needed
        // public ActivationRuleViewModel() { }
    }
}