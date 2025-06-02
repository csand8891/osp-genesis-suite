// RuleArchitect.DesktopClient/ViewModels/SpecCodeViewModel.cs
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RuleArchitect.DesktopClient.ViewModels
{
    public class SpecCodeViewModel : BaseViewModel
    {
        private int _originalId; // SoftwareOptionSpecificationCodeId (ID of this link/row)
        private int _specCodeDefinitionId; // ID of the linked SpecCodeDefinition (found or new)

        // New properties for user input
        private string _category = string.Empty;
        private string _specCodeNo = string.Empty;
        private string _specCodeBit = string.Empty;
        private string? _description;
        private bool _isDescriptionReadOnly = true; // Initially, assume it's read-only until checked or new

        // Existing properties
        private int? _softwareOptionActivationRuleId;
        private string? _specificInterpretation;
        private string? _activationRuleName; // For display

        // This might be less used now, or dynamically built
        private string? _specCodeDisplayName;


        public int OriginalId { get => _originalId; set => SetProperty(ref _originalId, value); }

        /// <summary>
        /// Gets or sets the ID of the associated SpecCodeDefinition.
        /// This is typically set by the parent ViewModel after a find/create operation.
        /// </summary>
        public int SpecCodeDefinitionId
        {
            get => _specCodeDefinitionId;
            set => SetProperty(ref _specCodeDefinitionId, value,
                onChanged: () => {
                    // Optionally update a display name if one is constructed based on this ID later
                    // OnPropertyChanged(nameof(SomeConsolidatedDisplayName));
                    ItemChangedCallback?.Invoke();
                });
        }

        // New Properties for direct input in the DataGrid
        public string Category
        {
            get => _category;
            set => SetProperty(ref _category, value, onChanged: () => ItemChangedCallback?.Invoke());
        }

        public string SpecCodeNo
        {
            get => _specCodeNo;
            set => SetProperty(ref _specCodeNo, value, onChanged: () => ItemChangedCallback?.Invoke());
        }

        public string SpecCodeBit
        {
            get => _specCodeBit;
            set => SetProperty(ref _specCodeBit, value, onChanged: () => ItemChangedCallback?.Invoke());
        }

        public string? Description
        {
            get => _description;
            set => SetProperty(ref _description, value, onChanged: () => ItemChangedCallback?.Invoke());
        }

        public bool IsDescriptionReadOnly
        {
            get => _isDescriptionReadOnly;
            set => SetProperty(ref _isDescriptionReadOnly, value); // No ItemChangedCallback needed for UI state
        }

        // Existing Properties
        public int? SoftwareOptionActivationRuleId
        {
            get => _softwareOptionActivationRuleId;
            set => SetProperty(ref _softwareOptionActivationRuleId, value,
                onChanged: () => {
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

        public string? ActivationRuleName
        {
            get => _activationRuleName;
            set => SetProperty(ref _activationRuleName, value);
        }

        /// <summary>
        /// Gets or sets a display name for the Spec Code, potentially constructed
        /// from its parts (Category, No, Bit, Description).
        /// The logic to set this would be in the parent ViewModel or after a 'check'.
        /// </summary>
        public string? SpecCodeDisplayName
        {
            get => _specCodeDisplayName;
            set => SetProperty(ref _specCodeDisplayName, value);
        }

        public SpecCodeViewModel()
        {
            // Default IsDescriptionReadOnly to true.
            // The parent ViewModel (EditSoftwareOptionViewModel) will manage this state
            // after checking if the SpecCodeDefinition exists.
            _isDescriptionReadOnly = true;
        }
    }
}