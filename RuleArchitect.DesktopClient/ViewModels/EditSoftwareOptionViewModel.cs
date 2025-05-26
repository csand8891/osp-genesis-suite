// File: RuleArchitect.DesktopClient/ViewModels/EditSoftwareOptionViewModel.cs
using GenesisSentry.Interfaces;
using RuleArchitect.ApplicationLogic.DTOs;
using RuleArchitect.ApplicationLogic.Interfaces;
using RuleArchitect.DesktopClient.Commands;
using RuleArchitect.Entities; // For mapping entities to ViewModels
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RuleArchitect.DesktopClient.ViewModels
{
    public class EditSoftwareOptionViewModel : BaseViewModel // Assumes BaseViewModel is in the same namespace or imported
    {
        private readonly ISoftwareOptionService _softwareOptionService;
        private readonly IAuthenticationStateProvider _authStateProvider;
        // Consider injecting services for lookups (e.g., ISpecCodeDefinitionService) if item VMs need to populate display names

        private int _softwareOptionId;
        private SoftwareOption? _originalSoftwareOption; // Store the original for complex change detection if needed

        private bool _isLoading;

        // --- Dirty Flags ---
        private bool _isScalarModified;
        private bool _isOptionNumbersModified;
        private bool _isRequirementsModified;
        private bool _isSpecCodesModified;
        private bool _isActivationRulesModified;

        // --- Scalar Properties (Same as before) ---
        private string _primaryName = string.Empty;
        private string? _alternativeNames;
        private string? _sourceFileName;
        private string? _primaryOptionNumberDisplay;
        private string? _notes;
        private string? _checkedBy;
        private DateTime? _checkedDate;
        private int? _controlSystemId;
        private int _version;
        private DateTime _lastModifiedDate;
        private string? _lastModifiedBy;

        // --- Collection Properties using Item ViewModels ---
        public ObservableCollection<OptionNumberViewModel> OptionNumbers { get; }
        public ObservableCollection<RequirementViewModel> Requirements { get; }
        public ObservableCollection<SpecCodeViewModel> SpecificationCodes { get; }
        public ObservableCollection<ActivationRuleViewModel> ActivationRules { get; }

        #region Public Properties (Bound to UI) - Scalar properties are the same as previous draft

        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value, () => ((RelayCommand)SaveCommand).RaiseCanExecuteChanged());
        }

        public string PrimaryName
        {
            get => _primaryName;
            set => SetProperty(ref _primaryName, value, MarkScalarAsModified);
        }
        public string? AlternativeNames { get => _alternativeNames; set => SetProperty(ref _alternativeNames, value, MarkScalarAsModified); }
        public string? SourceFileName { get => _sourceFileName; set => SetProperty(ref _sourceFileName, value, MarkScalarAsModified); }
        public string? PrimaryOptionNumberDisplay { get => _primaryOptionNumberDisplay; set => SetProperty(ref _primaryOptionNumberDisplay, value, MarkScalarAsModified); }
        public string? Notes { get => _notes; set => SetProperty(ref _notes, value, MarkScalarAsModified); }
        public string? CheckedBy { get => _checkedBy; set => SetProperty(ref _checkedBy, value, MarkScalarAsModified); }
        public DateTime? CheckedDate { get => _checkedDate; set => SetProperty(ref _checkedDate, value, MarkScalarAsModified); }
        public int? ControlSystemId { get => _controlSystemId; set => SetProperty(ref _controlSystemId, value, MarkScalarAsModified); }

        public int Version { get => _version; private set => SetProperty(ref _version, value); }
        public DateTime LastModifiedDate { get => _lastModifiedDate; private set => SetProperty(ref _lastModifiedDate, value); }
        public string? LastModifiedBy { get => _lastModifiedBy; private set => SetProperty(ref _lastModifiedBy, value); }

        #endregion

        // --- Commands ---
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; } // Example
        // Example commands for collection items
        public ICommand AddRequirementCommand { get; }
        public ICommand RemoveRequirementCommand { get; }
        private RequirementViewModel? _selectedRequirement;
        public RequirementViewModel? SelectedRequirement { get => _selectedRequirement; set => SetProperty(ref _selectedRequirement, value, () => ((RelayCommand)RemoveRequirementCommand).RaiseCanExecuteChanged()); }


        public EditSoftwareOptionViewModel(ISoftwareOptionService softwareOptionService, IAuthenticationStateProvider authStateProvider)
        {
            _softwareOptionService = softwareOptionService;
            _authStateProvider = authStateProvider;

            // Initialize Collections
            OptionNumbers = new ObservableCollection<OptionNumberViewModel>();
            Requirements = new ObservableCollection<RequirementViewModel>();
            SpecificationCodes = new ObservableCollection<SpecCodeViewModel>();
            ActivationRules = new ObservableCollection<ActivationRuleViewModel>();

            // Subscribe to CollectionChanged events to set dirty flags
            OptionNumbers.CollectionChanged += (s, e) => _isOptionNumbersModified = true;
            Requirements.CollectionChanged += (s, e) => _isRequirementsModified = true;
            SpecificationCodes.CollectionChanged += (s, e) => _isSpecCodesModified = true;
            ActivationRules.CollectionChanged += (s, e) => _isActivationRulesModified = true;

            SaveCommand = new RelayCommand(async () => await ExecuteSaveAsync(), () => !IsLoading);
            // CancelCommand = new RelayCommand(ExecuteCancel);

            // Example commands for Requirements collection
            AddRequirementCommand = new RelayCommand(ExecuteAddRequirement);
            RemoveRequirementCommand = new RelayCommand(ExecuteRemoveRequirement, () => SelectedRequirement != null);

        }

        private void MarkScalarAsModified() => _isScalarModified = true;
        private void MarkOptionNumbersAsModified() => _isOptionNumbersModified = true;
        private void MarkRequirementsAsModified() => _isRequirementsModified = true;
        private void MarkSpecCodesAsModified() => _isSpecCodesModified = true;
        private void MarkActivationRulesAsModified() => _isActivationRulesModified = true;

        public async Task LoadSoftwareOptionAsync(int softwareOptionId)
        {
            IsLoading = true;
            try
            {
                _originalSoftwareOption = await _softwareOptionService.GetSoftwareOptionByIdAsync(softwareOptionId);
                if (_originalSoftwareOption == null)
                {
                    Console.WriteLine($"Error: Software Option with ID {softwareOptionId} not found.");
                    // Handle UI accordingly (e.g., show message, disable save, allow navigation back)
                    return;
                }

                _softwareOptionId = _originalSoftwareOption.SoftwareOptionId;

                // Load Scalar Properties
                PrimaryName = _originalSoftwareOption.PrimaryName;
                AlternativeNames = _originalSoftwareOption.AlternativeNames;
                SourceFileName = _originalSoftwareOption.SourceFileName;
                PrimaryOptionNumberDisplay = _originalSoftwareOption.PrimaryOptionNumberDisplay;
                Notes = _originalSoftwareOption.Notes;
                CheckedBy = _originalSoftwareOption.CheckedBy;
                CheckedDate = _originalSoftwareOption.CheckedDate;
                ControlSystemId = _originalSoftwareOption.ControlSystemId;
                Version = _originalSoftwareOption.Version;
                LastModifiedDate = _originalSoftwareOption.LastModifiedDate;
                LastModifiedBy = _originalSoftwareOption.LastModifiedBy;

                // Load Collections (Map Entities to Item ViewModels and set ItemChangedCallback)
                OptionNumbers.Clear();
                _originalSoftwareOption.OptionNumberRegistries?.ToList().ForEach(onr =>
                {
                    var vm = new OptionNumberViewModel { OriginalId = onr.OptionNumberRegistryId, OptionNumber = onr.OptionNumber };
                    vm.ItemChangedCallback = MarkOptionNumbersAsModified;
                    OptionNumbers.Add(vm);
                });

                Requirements.Clear();
                _originalSoftwareOption.Requirements?.ToList().ForEach(r =>
                {
                    var vm = new RequirementViewModel
                    {
                        OriginalId = r.RequirementId,
                        RequirementType = r.RequirementType,
                        Condition = r.Condition,
                        GeneralRequiredValue = r.GeneralRequiredValue,
                        RequiredSoftwareOptionId = r.RequiredSoftwareOptionId,
                        RequiredSpecCodeDefinitionId = r.RequiredSpecCodeDefinitionId,
                        OspFileName = r.OspFileName,
                        OspFileVersion = r.OspFileVersion,
                        Notes = r.Notes,
                        // You would typically load display names here if needed
                        // RequiredSoftwareOptionName = r.RequiredSoftwareOption?.PrimaryName,
                        // RequiredSpecCodeName = r.RequiredSpecCodeDefinition?.Description // Or a formatted string
                    };
                    vm.ItemChangedCallback = MarkRequirementsAsModified;
                    Requirements.Add(vm);
                });

                SpecificationCodes.Clear();
                _originalSoftwareOption.SoftwareOptionSpecificationCodes?.ToList().ForEach(sosc =>
                {
                    var vm = new SpecCodeViewModel
                    {
                        OriginalId = sosc.SoftwareOptionSpecificationCodeId,
                        SpecCodeDefinitionId = sosc.SpecCodeDefinitionId,
                        SoftwareOptionActivationRuleId = sosc.SoftwareOptionActivationRuleId,
                        SpecificInterpretation = sosc.SpecificInterpretation,
                        // SpecCodeDisplayName = sosc.SpecCodeDefinition != null ? $"{sosc.SpecCodeDefinition.SpecCodeNo} - {sosc.SpecCodeDefinition.SpecCodeBit}" : "N/A",
                        // ActivationRuleName = sosc.SoftwareOptionActivationRule?.RuleName
                    };
                    vm.ItemChangedCallback = MarkSpecCodesAsModified;
                    SpecificationCodes.Add(vm);
                });

                ActivationRules.Clear();
                _originalSoftwareOption.SoftwareOptionActivationRules?.ToList().ForEach(ar =>
                {
                    var vm = new ActivationRuleViewModel
                    {
                        OriginalId = ar.SoftwareOptionActivationRuleId,
                        RuleName = ar.RuleName,
                        ActivationSetting = ar.ActivationSetting,
                        Notes = ar.Notes
                    };
                    vm.ItemChangedCallback = MarkActivationRulesAsModified;
                    ActivationRules.Add(vm);
                });

                ResetDirtyFlags(); // Reset flags after initial load
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading Software Option: {ex.Message}");
                // Consider showing an error message to the user
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ResetDirtyFlags()
        {
            _isScalarModified = false;
            _isOptionNumbersModified = false;
            _isRequirementsModified = false;
            _isSpecCodesModified = false;
            _isActivationRulesModified = false;
        }

        private async Task ExecuteSaveAsync()
        {
            if (_originalSoftwareOption == null) // Should not happen if Load was successful
            {
                Console.WriteLine("Cannot save, no Software Option loaded.");
                return;
            }

            IsLoading = true;

            var dto = new UpdateSoftwareOptionCommandDto
            {
                SoftwareOptionId = _softwareOptionId,
                // Always send current scalar values
                PrimaryName = this.PrimaryName,
                AlternativeNames = this.AlternativeNames,
                SourceFileName = this.SourceFileName,
                PrimaryOptionNumberDisplay = this.PrimaryOptionNumberDisplay,
                Notes = this.Notes,
                CheckedBy = this.CheckedBy,
                CheckedDate = this.CheckedDate,
                ControlSystemId = this.ControlSystemId,

                // Conditionally send collections based on dirty flags
                OptionNumbers = _isOptionNumbersModified ? OptionNumbers.Select(vm => new OptionNumberRegistryCreateDto { OptionNumber = vm.OptionNumber }).ToList() : null,
                Requirements = _isRequirementsModified ? Requirements.Select(vm => new RequirementCreateDto
                {
                    RequirementType = vm.RequirementType,
                    Condition = vm.Condition,
                    GeneralRequiredValue = vm.GeneralRequiredValue,
                    RequiredSoftwareOptionId = vm.RequiredSoftwareOptionId,
                    RequiredSpecCodeDefinitionId = vm.RequiredSpecCodeDefinitionId,
                    OspFileName = vm.OspFileName,
                    OspFileVersion = vm.OspFileVersion,
                    Notes = vm.Notes
                }).ToList() : null,
                SpecificationCodes = _isSpecCodesModified ? SpecificationCodes.Select(vm => new SoftwareOptionSpecificationCodeCreateDto
                {
                    SpecCodeDefinitionId = vm.SpecCodeDefinitionId,
                    SoftwareOptionActivationRuleId = vm.SoftwareOptionActivationRuleId,
                    SpecificInterpretation = vm.SpecificInterpretation
                }).ToList() : null,
                ActivationRules = _isActivationRulesModified ? ActivationRules.Select(vm => new SoftwareOptionActivationRuleCreateDto
                {
                    RuleName = vm.RuleName,
                    ActivationSetting = vm.ActivationSetting,
                    Notes = vm.Notes
                }).ToList() : null,
            };

            try
            {
                var updatedOption = await _softwareOptionService.UpdateSoftwareOptionAsync(dto, _authStateProvider.CurrentUser?.UserName ?? "UnknownUser");

                if (updatedOption != null)
                {
                    Console.WriteLine($"Successfully updated {updatedOption.PrimaryName} to version {updatedOption.Version}");
                    // Reload to reflect changes (like new Version #) and reset dirty flags
                    await LoadSoftwareOptionAsync(_softwareOptionId);
                }
                else
                {
                    Console.WriteLine("Failed to update Software Option (possibly not found or no changes detected by service).");
                    // If service returns null due to no changes, we might not need to reload.
                    // However, if it returns null due to "not found", that's an issue.
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving Software Option: {ex.Message}");
                // Show user-friendly error
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Example Collection Management Methods
        private void ExecuteAddRequirement()
        {
            var newReqVm = new RequirementViewModel();
            newReqVm.ItemChangedCallback = MarkRequirementsAsModified;
            Requirements.Add(newReqVm);
            // _isRequirementsModified will be true due to CollectionChanged
            // UI would typically open a dialog to populate the new RequirementViewModel
        }

        private void ExecuteRemoveRequirement()
        {
            if (SelectedRequirement != null)
            {
                Requirements.Remove(SelectedRequirement);
                // _isRequirementsModified will be true due to CollectionChanged
            }
        }

        // Implement similar Add/Remove methods for other collections as needed
    }
}