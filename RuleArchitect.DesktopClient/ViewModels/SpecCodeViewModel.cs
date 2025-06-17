// RuleArchitect.DesktopClient/ViewModels/SpecCodeViewModel.cs
using Microsoft.Extensions.DependencyInjection;
using RuleArchitect.Abstractions.Interfaces;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
        // NEW: Add a backing field and property for ControlSystemId
        private int? _controlSystemId;
        public int? ControlSystemId
        {
            get => _controlSystemId;
            set => SetProperty(ref _controlSystemId, value);
        }

        public int OriginalId { get => _originalId; set => SetProperty(ref _originalId, value); }
        private readonly IServiceScopeFactory _scopeFactory;
        public SpecCodeViewModel(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            // Default IsDescriptionReadOnly to true.
            _isDescriptionReadOnly = true;
        }

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
                    //ItemChangedCallback?.Invoke();
                });
        }

        // New Properties for direct input in the DataGrid
        public string Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        public string SpecCodeNo
        {
            get => _specCodeNo;
            set => SetProperty(ref _specCodeNo, value);
        }

        public string SpecCodeBit
        {
            get => _specCodeBit;
            set => SetProperty(ref _specCodeBit, value);
        }

        public string? Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
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
                    
                });
        }

        public string? SpecificInterpretation
        {
            get => _specificInterpretation;
            set => SetProperty(ref _specificInterpretation, value);
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

        public async Task CheckDefinitionAsync(IServiceScopeFactory scopeFactory)
        {
            // Clear any previous errors related to definition existence if using INotifyDataErrorInfo
            // ClearErrors(nameof(Description)); // If you want to add validation here

            if (!ControlSystemId.HasValue || ControlSystemId.Value <= 0 ||
                string.IsNullOrWhiteSpace(Category) ||
                string.IsNullOrWhiteSpace(SpecCodeNo) ||
                string.IsNullOrWhiteSpace(SpecCodeBit))
            {
                // Not enough information to perform a lookup
                IsDescriptionReadOnly = false; // Allow editing if incomplete
                Description = string.Empty; // Clear description if inputs are invalid
                SpecCodeDefinitionId = 0; // Indicate no definition found/linked
                return;
            }

            using (var scope = scopeFactory.CreateScope())
            {
                var softwareOptionService = scope.ServiceProvider.GetRequiredService<ISoftwareOptionService>();
                var foundSpecCodeDef = await softwareOptionService.FindSpecCodeDefinitionAsync(
                    ControlSystemId.Value, Category, SpecCodeNo, SpecCodeBit);

                if (foundSpecCodeDef != null)
                {
                    Description = foundSpecCodeDef.Description;
                    SpecCodeDefinitionId = foundSpecCodeDef.SpecCodeDefinitionId;
                    IsDescriptionReadOnly = true; // Description is now read-only
                }
                else
                {
                    // No existing definition found, allow user to input a new description
                    IsDescriptionReadOnly = false;
                    SpecCodeDefinitionId = 0; // Indicate no definition found/linked (new spec code)
                    // Do NOT clear Description here if user has already typed something.
                    // Only clear if you want strict auto-clear on no match.
                }
            }
        }
    }
}