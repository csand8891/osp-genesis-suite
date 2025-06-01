// File: RuleArchitect.DesktopClient/ViewModels/EditSoftwareOptionViewModel.cs
using GenesisSentry.Interfaces;
using RuleArchitect.ApplicationLogic.DTOs;
using RuleArchitect.ApplicationLogic.Interfaces;
using RuleArchitect.DesktopClient.Commands;
using RuleArchitect.Entities;
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
    public class EditSoftwareOptionViewModel : BaseViewModel
    {
        private readonly ISoftwareOptionService _softwareOptionService;
        private readonly IAuthenticationStateProvider _authStateProvider;

        private int _softwareOptionId;
        private SoftwareOption? _originalSoftwareOption;
        private bool _isNewSoftwareOption; // Flag to distinguish Add vs Edit mode

        private bool _isLoading;

        private bool _isScalarModified;
        private bool _isOptionNumbersModified;
        private bool _isRequirementsModified;
        private bool _isSpecCodesModified;
        private bool _isActivationRulesModified;

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

        public ObservableCollection<OptionNumberViewModel> OptionNumbers { get; }
        public ObservableCollection<RequirementViewModel> Requirements { get; }
        public ObservableCollection<SpecCodeViewModel> SpecificationCodes { get; }
        public ObservableCollection<ActivationRuleViewModel> ActivationRules { get; }

        // --- NEW --- Title Property for the View
        public string ViewTitle => _isNewSoftwareOption
                                   ? "Create New Software Option"
                                   : $"Edit Software Option (Name: {PrimaryName})";
        // Alternative for edit: : $"Edit Software Option (ID: {SoftwareOptionId})";

        #region Public Properties
        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value, () => ((RelayCommand)SaveCommand).RaiseCanExecuteChanged());
        }

        public string PrimaryName
        {
            get => _primaryName;
            set
            {
                // Use the SetProperty overload that takes an Action for additional logic
                SetProperty(ref _primaryName, value,
                    onChanged: () => {
                        MarkScalarAsModified();
                        OnPropertyChanged(nameof(ViewTitle)); // Notify ViewTitle changed
                    }
                );
            }
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

        public int SoftwareOptionId
        {
            get => _softwareOptionId;
            private set
            {
                // If ID changing could affect the title (e.g., if title uses ID)
                SetProperty(ref _softwareOptionId, value, onChanged: () => OnPropertyChanged(nameof(ViewTitle)));
            }
        }
        #endregion

        private SpecCodeViewModel? _selectedSpecificationCode;
        public SpecCodeViewModel? SelectedSpecificationCode
        {
            get => _selectedSpecificationCode;
            set => SetProperty(ref _selectedSpecificationCode, value, () => ((RelayCommand)RemoveSpecificationCodeCommand).RaiseCanExecuteChanged());
        }

        public ObservableCollection<SpecCodeDefinitionLookupDto> AvailableSpecCodeDefinitions { get; } // Placeholder DTO
        public ObservableCollection<ActivationRuleLookupDto> AvailableActivationRules { get; }     // Placeholder DTO

        public ICommand AddSpecificationCodeCommand { get; }
        public ICommand RemoveSpecificationCodeCommand { get; }
        public ICommand SaveCommand { get; }
        // public ICommand CancelCommand { get; } // You had this commented out, uncomment if needed
        public ICommand AddRequirementCommand { get; }
        public ICommand RemoveRequirementCommand { get; }
        private RequirementViewModel? _selectedRequirement;
        public RequirementViewModel? SelectedRequirement { get => _selectedRequirement; set => SetProperty(ref _selectedRequirement, value, () => ((RelayCommand)RemoveRequirementCommand).RaiseCanExecuteChanged()); }

        /// <summary>
        /// Constructor used when creating a NEW Software Option.
        /// SoftwareOptionsViewModel will call this.
        /// </summary>
        // In EditSoftwareOptionViewModel.cs constructor

        public EditSoftwareOptionViewModel(ISoftwareOptionService softwareOptionService, IAuthenticationStateProvider authStateProvider)
        {
            _softwareOptionService = softwareOptionService ?? throw new ArgumentNullException(nameof(softwareOptionService));
            _authStateProvider = authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));

            _isNewSoftwareOption = true;

            OptionNumbers = new ObservableCollection<OptionNumberViewModel>();
            Requirements = new ObservableCollection<RequirementViewModel>();
            SpecificationCodes = new ObservableCollection<SpecCodeViewModel>();
            ActivationRules = new ObservableCollection<ActivationRuleViewModel>();

            AvailableSpecCodeDefinitions = new ObservableCollection<SpecCodeDefinitionLookupDto>();
            AvailableActivationRules = new ObservableCollection<ActivationRuleLookupDto>();

            SubscribeToCollectionChanges(); // Make sure this is called

            SaveCommand = new RelayCommand(async () => await ExecuteSaveAsync(), () => !IsLoading);

            // --- Ensure these commands for collections are initialized ---
            AddOptionNumberCommand = new RelayCommand(ExecuteAddOptionNumber); // <<<< THIS LINE
            RemoveOptionNumberCommand = new RelayCommand(ExecuteRemoveOptionNumber, () => SelectedOptionNumber != null);

            AddRequirementCommand = new RelayCommand(ExecuteAddRequirement); // <<<< THIS LINE (and its method)
            RemoveRequirementCommand = new RelayCommand(ExecuteRemoveRequirement, () => SelectedRequirement != null);

            // TODO: Initialize Add/Remove commands for SpecificationCodes
            AddSpecificationCodeCommand = new RelayCommand(ExecuteAddSpecificationCode);
            RemoveSpecificationCodeCommand = new RelayCommand(ExecuteRemoveSpecificationCode, () => SelectedSpecificationCode != null);

            // TODO: Initialize Add/Remove commands for ActivationRules
            // AddActivationRuleCommand = new RelayCommand(ExecuteAddActivationRule);
            // RemoveActivationRuleCommand = new RelayCommand(ExecuteRemoveActivationRule, () => SelectedActivationRule != null);

            // Initialize properties for a new item
            PrimaryName = "";
            Version = 1;
            LastModifiedDate = DateTime.UtcNow;
            LastModifiedBy = _authStateProvider.CurrentUser?.UserName ?? "System";
            ResetDirtyFlags();
            OnPropertyChanged(nameof(ViewTitle));
        }

        public class SpecCodeDefinitionLookupDto { public int Id { get; set; } public string DisplayName { get; set; } }
        public class ActivationRuleLookupDto { public int Id { get; set; } public string DisplayName { get; set; } }

        private void SubscribeToCollectionChanges()
        {
            OptionNumbers.CollectionChanged += (s, e) => MarkCollectionAsModified(ref _isOptionNumbersModified);
            Requirements.CollectionChanged += (s, e) => MarkCollectionAsModified(ref _isRequirementsModified);
            SpecificationCodes.CollectionChanged += (s, e) => MarkCollectionAsModified(ref _isSpecCodesModified);
            ActivationRules.CollectionChanged += (s, e) => MarkCollectionAsModified(ref _isActivationRulesModified);
        }

        private void MarkScalarAsModified() => _isScalarModified = true;
        private void MarkCollectionAsModified(ref bool flag) => flag = true; // Helper
        private void MarkOptionNumbersAsModified() => MarkCollectionAsModified(ref _isOptionNumbersModified);
        private void MarkRequirementsAsModified() => MarkCollectionAsModified(ref _isRequirementsModified);
        private void MarkSpecCodesAsModified() => MarkCollectionAsModified(ref _isSpecCodesModified);
        private void MarkActivationRulesAsModified() => MarkCollectionAsModified(ref _isActivationRulesModified);

        private async Task LoadLookupDataAsync()
        {
            // This method would be called from the constructor (non-blocking) or from LoadSoftwareOptionAsync
            // It needs to populate AvailableSpecCodeDefinitions and AvailableActivationRules
            // For example, using hypothetical services:
            /*
            var specDefs = await _specCodeDefService.GetAllAsync(); // Assuming a service and DTOs
            AvailableSpecCodeDefinitions.Clear();
            foreach(var def in specDefs) { AvailableSpecCodeDefinitions.Add(new SpecCodeDefinitionLookupDto { Id = def.Id, DisplayName = $"{def.SpecCodeNo}/{def.SpecCodeBit} - {def.Description}" }); }

            // Similarly for Activation Rules, but these are often specific to the current SoftwareOption
            // For now, we'll assume they might be more global or loaded as part of the SO.
            // If they are part of SoftwareOption.SoftwareOptionActivationRules, you might populate
            // AvailableActivationRules from the _originalSoftwareOption.SoftwareOptionActivationRules in LoadSoftwareOptionAsync
            */
            // For now, let's add some dummy data for testing the ComboBoxes
            if (!AvailableSpecCodeDefinitions.Any())
            {
                AvailableSpecCodeDefinitions.Add(new SpecCodeDefinitionLookupDto { Id = 1, DisplayName = "SCD001 - Test Definition 1" });
                AvailableSpecCodeDefinitions.Add(new SpecCodeDefinitionLookupDto { Id = 2, DisplayName = "SCD002 - Test Definition 2" });
            }
            if (!AvailableActivationRules.Any())
            {
                AvailableActivationRules.Add(new ActivationRuleLookupDto { Id = 1, DisplayName = "AR001 - Test Rule 1" });
                AvailableActivationRules.Add(new ActivationRuleLookupDto { Id = 2, DisplayName = "AR002 - Test Rule 2" });
            }
            OnPropertyChanged(nameof(AvailableSpecCodeDefinitions));
            OnPropertyChanged(nameof(AvailableActivationRules));
        }
        public async Task LoadSoftwareOptionAsync(int softwareOptionIdToLoad)
        {
            IsLoading = true;
            _isNewSoftwareOption = false; // This instance is now for editing an existing option
            if (!AvailableSpecCodeDefinitions.Any() || !AvailableActivationRules.Any()) // Simple check
            {
                await LoadLookupDataAsync();
            }
            try
            {
                _originalSoftwareOption = await _softwareOptionService.GetSoftwareOptionByIdAsync(softwareOptionIdToLoad);
                if (_originalSoftwareOption == null)
                {
                    Console.WriteLine($"Error: Software Option with ID {softwareOptionIdToLoad} not found.");
                    // TODO: Notify user, perhaps through a message on the view
                    PrimaryName = "ERROR: Not Found"; // Indicate error in UI
                    OnPropertyChanged(nameof(ViewTitle));
                    IsLoading = false;
                    return;
                }
                _originalSoftwareOption = await _softwareOptionService.GetSoftwareOptionByIdAsync(softwareOptionIdToLoad);
                if (_originalSoftwareOption == null)
                {
                    // ... (handle not found) ...
                    Console.WriteLine($"Error: Software Option with ID {softwareOptionIdToLoad} not found.");
                    PrimaryName = "ERROR: Not Found";
                    OnPropertyChanged(nameof(ViewTitle));
                    IsLoading = false;
                    return;
                }
                SoftwareOptionId = _originalSoftwareOption.SoftwareOptionId;
                // Set PrimaryName first as ViewTitle depends on it for edit mode
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

                // Load Collections
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
                    var vm = new RequirementViewModel { /* map all properties */ };
                    vm.OriginalId = r.RequirementId;
                    vm.RequirementType = r.RequirementType;
                    vm.Condition = r.Condition;
                    vm.GeneralRequiredValue = r.GeneralRequiredValue;
                    vm.RequiredSoftwareOptionId = r.RequiredSoftwareOptionId;
                    vm.RequiredSpecCodeDefinitionId = r.RequiredSpecCodeDefinitionId;
                    vm.OspFileName = r.OspFileName;
                    vm.OspFileVersion = r.OspFileVersion;
                    vm.Notes = r.Notes;
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
                        // Populate display names based on IDs and lookup collections
                        SpecCodeDisplayName = AvailableSpecCodeDefinitions.FirstOrDefault(def => def.Id == sosc.SpecCodeDefinitionId)?.DisplayName,
                        ActivationRuleName = AvailableActivationRules.FirstOrDefault(rule => rule.Id == sosc.SoftwareOptionActivationRuleId)?.DisplayName
                    };
                    vm.ItemChangedCallback = MarkSpecCodesAsModified;
                    SpecificationCodes.Add(vm);
                });
                ActivationRules.Clear();
                _originalSoftwareOption.SoftwareOptionActivationRules?.ToList().ForEach(ar =>
                {
                    var vm = new ActivationRuleViewModel { /* map all properties */ };
                    vm.OriginalId = ar.SoftwareOptionActivationRuleId;
                    vm.RuleName = ar.RuleName;
                    vm.ActivationSetting = ar.ActivationSetting;
                    vm.Notes = ar.Notes;
                    vm.ItemChangedCallback = MarkActivationRulesAsModified;
                    ActivationRules.Add(vm);
                });

                ResetDirtyFlags();
                OnPropertyChanged(nameof(ViewTitle)); // Update title explicitly after all properties are set for edit mode
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading Software Option: {ex.Message}");
                PrimaryName = "ERROR: Load Failed";
                OnPropertyChanged(nameof(ViewTitle));
                // Consider showing an error message to the user via INotificationService
            }
            finally
            {
                IsLoading = false;
            }
        }

        // --- NEW: Execute methods for Specification Codes ---
        private void ExecuteAddSpecificationCode()
        {
            System.Diagnostics.Debug.WriteLine("ExecuteAddSpecificationCode called.");
            var newSpecCode = new SpecCodeViewModel();
            newSpecCode.ItemChangedCallback = MarkSpecCodesAsModified; // If props in SpecCodeViewModel are editable
            SpecificationCodes.Add(newSpecCode);
            MarkSpecCodesAsModified(); // Mark collection as modified
            System.Diagnostics.Debug.WriteLine($"SpecificationCodes count: {SpecificationCodes.Count}");
        }

        private void ExecuteRemoveSpecificationCode()
        {
            if (SelectedSpecificationCode != null)
            {
                SpecificationCodes.Remove(SelectedSpecificationCode);
                MarkSpecCodesAsModified(); // Mark collection as modified
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

        public async Task<bool> ExecuteSaveAsync()
        {
            if (string.IsNullOrWhiteSpace(PrimaryName))
            {
                Console.WriteLine("Cannot save, Primary Name is required.");
                // TODO: Use INotificationService to show validation error
                return false;
            }

            IsLoading = true;
            string currentUser = _authStateProvider.CurrentUser?.UserName ?? "System";

            try
            {
                if (_isNewSoftwareOption)
                {
                    var createDto = new CreateSoftwareOptionCommandDto
                    {
                        PrimaryName = this.PrimaryName,
                        AlternativeNames = this.AlternativeNames,
                        SourceFileName = this.SourceFileName,
                        PrimaryOptionNumberDisplay = this.PrimaryOptionNumberDisplay,
                        Notes = this.Notes,
                        //CheckedBy = this.CheckedBy,
                        //CheckedDate = this.CheckedDate,
                        ControlSystemId = this.ControlSystemId,
                        OptionNumbers = this.OptionNumbers.Select(vm => new OptionNumberRegistryCreateDto { OptionNumber = vm.OptionNumber }).ToList(),
                        Requirements = this.Requirements.Select(vm => new RequirementCreateDto { RequirementType = vm.RequirementType, Condition = vm.Condition, GeneralRequiredValue = vm.GeneralRequiredValue, RequiredSoftwareOptionId = vm.RequiredSoftwareOptionId, RequiredSpecCodeDefinitionId = vm.RequiredSpecCodeDefinitionId, OspFileName = vm.OspFileName, OspFileVersion = vm.OspFileVersion, Notes = vm.Notes }).ToList(),
                        SpecificationCodes = this.SpecificationCodes.Select(vm => new SoftwareOptionSpecificationCodeCreateDto
                        {
                            SpecCodeDefinitionId = vm.SpecCodeDefinitionId,
                            SoftwareOptionActivationRuleId = vm.SoftwareOptionActivationRuleId,
                            SpecificInterpretation = vm.SpecificInterpretation
                        }).ToList(),
                        ActivationRules = this.ActivationRules.Select(vm => new SoftwareOptionActivationRuleCreateDto { RuleName = vm.RuleName, ActivationSetting = vm.ActivationSetting, Notes = vm.Notes }).ToList()
                    };

                    var createdOption = await _softwareOptionService.CreateSoftwareOptionAsync(createDto, currentUser);
                    if (createdOption != null)
                    {
                        // Update ViewModel from created entity
                        SoftwareOptionId = createdOption.SoftwareOptionId;
                        Version = createdOption.Version;
                        LastModifiedDate = createdOption.LastModifiedDate;
                        LastModifiedBy = createdOption.LastModifiedBy;
                        // PrimaryName setter will update ViewTitle
                        PrimaryName = createdOption.PrimaryName; // Important to re-set to trigger ViewTitle update potentially

                        _isNewSoftwareOption = false; // It's now an existing item
                        _originalSoftwareOption = createdOption; // Store for subsequent edits
                        ResetDirtyFlags();
                        IsLoading = false;
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Failed to create Software Option (service returned null).");
                        // TODO: Use INotificationService
                        IsLoading = false;
                        return false;
                    }
                }
                else // Updating existing Software Option
                {
                    if (_originalSoftwareOption == null && !_isNewSoftwareOption)
                    {
                        Console.WriteLine("Cannot update, original Software Option reference is missing for an existing item.");
                        IsLoading = false;
                        return false;
                    }

                    if (!_isScalarModified && !_isOptionNumbersModified && !_isRequirementsModified && !_isSpecCodesModified && !_isActivationRulesModified)
                    {
                        Console.WriteLine("No changes detected to save.");
                        IsLoading = false;
                        return true; // No changes, but operation is "complete" in a sense
                    }

                    var updateDto = new UpdateSoftwareOptionCommandDto
                    {
                        SoftwareOptionId = this.SoftwareOptionId,
                        PrimaryName = this.PrimaryName,
                        AlternativeNames = this.AlternativeNames,
                        SourceFileName = this.SourceFileName,
                        PrimaryOptionNumberDisplay = this.PrimaryOptionNumberDisplay,
                        Notes = this.Notes,
                        CheckedBy = this.CheckedBy,
                        CheckedDate = this.CheckedDate,
                        ControlSystemId = this.ControlSystemId,
                        OptionNumbers = _isOptionNumbersModified ? this.OptionNumbers.Select(vm => new OptionNumberRegistryCreateDto { OptionNumber = vm.OptionNumber }).ToList() : null,
                        Requirements = _isRequirementsModified ? this.Requirements.Select(vm => new RequirementCreateDto { RequirementType = vm.RequirementType, Condition = vm.Condition, GeneralRequiredValue = vm.GeneralRequiredValue, RequiredSoftwareOptionId = vm.RequiredSoftwareOptionId, RequiredSpecCodeDefinitionId = vm.RequiredSpecCodeDefinitionId, OspFileName = vm.OspFileName, OspFileVersion = vm.OspFileVersion, Notes = vm.Notes }).ToList() : null,
                        SpecificationCodes = _isSpecCodesModified ? this.SpecificationCodes.Select(vm => new SoftwareOptionSpecificationCodeCreateDto
                        {
                            SpecCodeDefinitionId = vm.SpecCodeDefinitionId,
                            SoftwareOptionActivationRuleId = vm.SoftwareOptionActivationRuleId,
                            SpecificInterpretation = vm.SpecificInterpretation
                        }).ToList() : null,
                        ActivationRules = _isActivationRulesModified ? this.ActivationRules.Select(vm => new SoftwareOptionActivationRuleCreateDto { RuleName = vm.RuleName, ActivationSetting = vm.ActivationSetting, Notes = vm.Notes }).ToList() : null
                    };

                    var updatedOption = await _softwareOptionService.UpdateSoftwareOptionAsync(updateDto, currentUser);
                    if (updatedOption != null)
                    {
                        // Reload data to reflect any server-side changes (like version increment) and reset dirty flags
                        await LoadSoftwareOptionAsync(this.SoftwareOptionId);
                        IsLoading = false;
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Failed to update Software Option (service returned null or no changes made).");
                        // Even if service indicates no DB change, reloading can reset UI state/dirty flags
                        await LoadSoftwareOptionAsync(this.SoftwareOptionId);
                        IsLoading = false;
                        return false; // Consider if this should be true if service said "no effective change"
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving Software Option: {ex.ToString()}");
                // TODO: Use INotificationService
                IsLoading = false;
                return false;
            }
        }
        private OptionNumberViewModel? _selectedOptionNumber;
        public OptionNumberViewModel? SelectedOptionNumber
        {
            get => _selectedOptionNumber;
            set => SetProperty(ref _selectedOptionNumber, value, () => ((RelayCommand)RemoveOptionNumberCommand).RaiseCanExecuteChanged());
        }

        public ICommand AddOptionNumberCommand { get; }
        // In constructor:
        // AddOptionNumberCommand = new RelayCommand(ExecuteAddOptionNumber);
        private void ExecuteAddOptionNumber()
        {
            System.Diagnostics.Debug.WriteLine("ExecuteAddOptionNumber called."); // Add for debugging
            var newOptionNumber = new OptionNumberViewModel { OptionNumber = "New Option #" }; // Initialize with a placeholder

            // If OptionNumberViewModel properties can change AND affect the 'modified' state of the whole SO:
            // newOptionNumber.ItemChangedCallback = MarkOptionNumbersAsModified; 

            OptionNumbers.Add(newOptionNumber);
            MarkOptionNumbersAsModified(); // To flag that the OptionNumbers collection has changed
            System.Diagnostics.Debug.WriteLine($"OptionNumbers count: {OptionNumbers.Count}");
        }

        public ICommand RemoveOptionNumberCommand { get; }
        // In constructor:
        // RemoveOptionNumberCommand = new RelayCommand(ExecuteRemoveOptionNumber, () => SelectedOptionNumber != null);
        private void ExecuteRemoveOptionNumber()
        {
            if (SelectedOptionNumber != null)
            {
                OptionNumbers.Remove(SelectedOptionNumber);
                MarkOptionNumbersAsModified(); // To flag the collection itself as changed
            }
        }
        private void ExecuteAddRequirement()
        {
            var newReqVm = new RequirementViewModel();
            newReqVm.ItemChangedCallback = MarkRequirementsAsModified;
            Requirements.Add(newReqVm);
        }

        private void ExecuteRemoveRequirement()
        {
            if (SelectedRequirement != null)
            {
                Requirements.Remove(SelectedRequirement);
                MarkRequirementsAsModified();
            }
        }
    }
}