// File: RuleArchitect.DesktopClient/ViewModels/EditSoftwareOptionViewModel.cs
using GenesisSentry.Interfaces;
using RuleArchitect.ApplicationLogic.DTOs;
using RuleArchitect.ApplicationLogic.Interfaces;
using RuleArchitect.DesktopClient.Commands; // For RelayCommand
using RuleArchitect.DesktopClient.Views;  // For EditSpecCodeDialog
using RuleArchitect.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows; // For MessageBox, Window
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection; // For IServiceProvider

namespace RuleArchitect.DesktopClient.ViewModels
{
    public class EditSoftwareOptionViewModel : BaseViewModel
    {
        private readonly ISoftwareOptionService _softwareOptionService;
        private readonly IAuthenticationStateProvider _authStateProvider;
        private readonly IServiceScopeFactory _scopeFactory; // For resolving dialog views

        // --- Existing private fields ---
        private int _softwareOptionId;
        private SoftwareOption? _originalSoftwareOption;
        private bool _isNewSoftwareOption;
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

        public ObservableCollection<ControlSystemLookupDto> AvailableControlSystems { get; }
        public ObservableCollection<ActivationRuleLookupDto> AvailableActivationRules { get; }


        public string ViewTitle => _isNewSoftwareOption
                                   ? "Create New Software Option"
                                   : $"Edit Software Option (Name: {PrimaryName})";

        #region Public Properties
        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value, () => ((RelayCommand)SaveCommand).RaiseCanExecuteChanged());
        }

        public string PrimaryName
        {
            get => _primaryName;
            set => SetProperty(ref _primaryName, value,
                onChanged: () => {
                    MarkScalarAsModified();
                    OnPropertyChanged(nameof(ViewTitle));
                });
        }
        public string? AlternativeNames { get => _alternativeNames; set => SetProperty(ref _alternativeNames, value, MarkScalarAsModified); }
        public string? SourceFileName { get => _sourceFileName; set => SetProperty(ref _sourceFileName, value, MarkScalarAsModified); }
        public string? PrimaryOptionNumberDisplay { get => _primaryOptionNumberDisplay; set => SetProperty(ref _primaryOptionNumberDisplay, value, MarkScalarAsModified); }
        public string? Notes { get => _notes; set => SetProperty(ref _notes, value, MarkScalarAsModified); }
        public string? CheckedBy { get => _checkedBy; set => SetProperty(ref _checkedBy, value, MarkScalarAsModified); }
        public DateTime? CheckedDate { get => _checkedDate; set => SetProperty(ref _checkedDate, value, MarkScalarAsModified); }
        public int? ControlSystemId
        {
            get => _controlSystemId;
            set
            {
                if (SetProperty(ref _controlSystemId, value, MarkScalarAsModified))
                {
                    // TODO: Potentially refresh or validate SpecificationCodes if ControlSystem changes
                    // For now, this ensures the change is tracked.
                }
            }
        }

        public int Version { get => _version; private set => SetProperty(ref _version, value); }
        public DateTime LastModifiedDate { get => _lastModifiedDate; private set => SetProperty(ref _lastModifiedDate, value); }
        public string? LastModifiedBy { get => _lastModifiedBy; private set => SetProperty(ref _lastModifiedBy, value); }

        public int SoftwareOptionId
        {
            get => _softwareOptionId;
            private set => SetProperty(ref _softwareOptionId, value, onChanged: () => OnPropertyChanged(nameof(ViewTitle)));
        }
        #endregion

        private SpecCodeViewModel? _selectedSpecificationCode;
        public SpecCodeViewModel? SelectedSpecificationCode
        {
            get => _selectedSpecificationCode;
            set
            {
                if (SetProperty(ref _selectedSpecificationCode, value))
                {
                    ((RelayCommand)RemoveSpecificationCodeCommand).RaiseCanExecuteChanged();
                    // Corrected: RelayCommand is not generic in your implementation
                    ((RelayCommand)EditSpecificationCodeCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public ICommand AddSpecificationCodeCommand { get; }
        public ICommand EditSpecificationCodeCommand { get; } // Changed from RelayCommand<T>
        public ICommand RemoveSpecificationCodeCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand AddOptionNumberCommand { get; }
        public ICommand RemoveOptionNumberCommand { get; }
        public ICommand AddRequirementCommand { get; }
        public ICommand RemoveRequirementCommand { get; }

        private OptionNumberViewModel? _selectedOptionNumber;
        public OptionNumberViewModel? SelectedOptionNumber { get => _selectedOptionNumber; set => SetProperty(ref _selectedOptionNumber, value, () => ((RelayCommand)RemoveOptionNumberCommand).RaiseCanExecuteChanged()); }

        private RequirementViewModel? _selectedRequirement;
        public RequirementViewModel? SelectedRequirement { get => _selectedRequirement; set => SetProperty(ref _selectedRequirement, value, () => ((RelayCommand)RemoveRequirementCommand).RaiseCanExecuteChanged()); }

        // Parameterless constructor for XAML Designer (XDG0010 fix)
        public EditSoftwareOptionViewModel()
        {
            // Design-time only constructor
            // Initialize collections to avoid NullReferenceExceptions in designer
            OptionNumbers = new ObservableCollection<OptionNumberViewModel>();
            Requirements = new ObservableCollection<RequirementViewModel>();
            SpecificationCodes = new ObservableCollection<SpecCodeViewModel>();
            ActivationRules = new ObservableCollection<ActivationRuleViewModel>();
            AvailableControlSystems = new ObservableCollection<ControlSystemLookupDto>();
            AvailableActivationRules = new ObservableCollection<ActivationRuleLookupDto>();

            // Dummy data for designer
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
            {
                PrimaryName = "Sample Software Option (Design)";
                ControlSystemId = 1;
                AvailableControlSystems.Add(new ControlSystemLookupDto { ControlSystemId = 1, Name = "Design CS P300" });
                AvailableActivationRules.Add(new ActivationRuleLookupDto { Id = 1, DisplayName = "Design Rule 1" });
                SpecificationCodes.Add(new SpecCodeViewModel { Category = "CAT D", SpecCodeNo = "S001", SpecCodeBit = "B01", Description = "Design Spec Code" });
            }
        }


        public EditSoftwareOptionViewModel(
            ISoftwareOptionService softwareOptionService,
            IAuthenticationStateProvider authStateProvider,
            IServiceScopeFactory scopeFactory) // Added IServiceProvider
        {
            _softwareOptionService = softwareOptionService ?? throw new ArgumentNullException(nameof(softwareOptionService));
            _authStateProvider = authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

            _isNewSoftwareOption = true;

            OptionNumbers = new ObservableCollection<OptionNumberViewModel>();
            Requirements = new ObservableCollection<RequirementViewModel>();
            SpecificationCodes = new ObservableCollection<SpecCodeViewModel>();
            ActivationRules = new ObservableCollection<ActivationRuleViewModel>();

            AvailableControlSystems = new ObservableCollection<ControlSystemLookupDto>();
            AvailableActivationRules = new ObservableCollection<ActivationRuleLookupDto>();

            SubscribeToCollectionChanges();

            SaveCommand = new RelayCommand(async () => await ExecuteSaveAsync(), () => !IsLoading);

            AddOptionNumberCommand = new RelayCommand(ExecuteAddOptionNumber);
            RemoveOptionNumberCommand = new RelayCommand(ExecuteRemoveOptionNumber, () => SelectedOptionNumber != null);
            AddRequirementCommand = new RelayCommand(ExecuteAddRequirement);
            RemoveRequirementCommand = new RelayCommand(ExecuteRemoveRequirement, () => SelectedRequirement != null);

            AddSpecificationCodeCommand = new RelayCommand(ExecuteShowSpecCodeDialogForAdd);
            // Corrected: RelayCommand is not generic. Parameter will be object.
            EditSpecificationCodeCommand = new RelayCommand(param => ExecuteShowSpecCodeDialogForEdit(param as SpecCodeViewModel), param => param is SpecCodeViewModel);
            RemoveSpecificationCodeCommand = new RelayCommand(ExecuteRemoveSpecificationCode, () => SelectedSpecificationCode != null);

            PrimaryName = "";
            Version = 1;
            LastModifiedDate = DateTime.UtcNow;
            LastModifiedBy = _authStateProvider.CurrentUser?.UserName ?? "System";
            ResetDirtyFlags();
            OnPropertyChanged(nameof(ViewTitle));

            _ = LoadInitialLookupsAsync();
        }

        // This inner DTO class was for placeholder, move to ApplicationLogic.DTOs if used by service
        // public class SpecCodeDefinitionLookupDto { public int Id { get; set; } public string DisplayName { get; set; } }
        public class ActivationRuleLookupDto { public int Id { get; set; } public string DisplayName { get; set; } } // Should be populated from service


        private async Task LoadInitialLookupsAsync()
        {
            // ... (implementation as before, ensure AvailableActivationRules are loaded properly)
            // For now, keeping dummy data for ActivationRuleLookupDto for simplicity if service method isn't ready
            IsLoading = true;
            try
            {
                var controlSystems = await _softwareOptionService.GetControlSystemLookupsAsync();
                Application.Current.Dispatcher.Invoke(() => { // Ensure UI updates on UI thread
                    AvailableControlSystems.Clear();
                    foreach (var cs in controlSystems) AvailableControlSystems.Add(cs);
                });

                // Placeholder for actual ActivationRule loading logic
                if (!AvailableActivationRules.Any())
                {
                    AvailableActivationRules.Add(new ActivationRuleLookupDto { Id = 1, DisplayName = "AR001 - Test Rule 1" });
                    AvailableActivationRules.Add(new ActivationRuleLookupDto { Id = 2, DisplayName = "AR002 - Test Rule 2" });
                }
                OnPropertyChanged(nameof(AvailableControlSystems));
                OnPropertyChanged(nameof(AvailableActivationRules));
            }
            catch (Exception ex) { Console.WriteLine($"Error loading initial lookups: {ex.Message}"); }
            finally { IsLoading = false; }
        }

        public async Task LoadSoftwareOptionAsync(int softwareOptionIdToLoad)
        {
            IsLoading = true;
            _isNewSoftwareOption = false;
            if (!AvailableControlSystems.Any() || !AvailableActivationRules.Any()) // Ensure lookups are loaded
            {
                await LoadInitialLookupsAsync();
            }

            try
            {
                _originalSoftwareOption = await _softwareOptionService.GetSoftwareOptionByIdAsync(softwareOptionIdToLoad);
                if (_originalSoftwareOption == null)
                {
                    PrimaryName = "ERROR: Not Found"; OnPropertyChanged(nameof(ViewTitle)); IsLoading = false; return;
                }

                SoftwareOptionId = _originalSoftwareOption.SoftwareOptionId;
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

                OptionNumbers.Clear();
                _originalSoftwareOption.OptionNumberRegistries?.ToList().ForEach(onr => OptionNumbers.Add(new OptionNumberViewModel { OriginalId = onr.OptionNumberRegistryId, OptionNumber = onr.OptionNumber, ItemChangedCallback = MarkOptionNumbersAsModified }));

                Requirements.Clear();
                _originalSoftwareOption.Requirements?.ToList().ForEach(r => Requirements.Add(new RequirementViewModel { OriginalId = r.RequirementId, RequirementType = r.RequirementType, Condition = r.Condition, GeneralRequiredValue = r.GeneralRequiredValue, RequiredSoftwareOptionId = r.RequiredSoftwareOptionId, RequiredSpecCodeDefinitionId = r.RequiredSpecCodeDefinitionId, OspFileName = r.OspFileName, OspFileVersion = r.OspFileVersion, Notes = r.Notes, ItemChangedCallback = MarkRequirementsAsModified }));

                SpecificationCodes.Clear();
                _originalSoftwareOption.SoftwareOptionSpecificationCodes?.ToList().ForEach(sosc =>
                {
                    var vm = new SpecCodeViewModel
                    {
                        OriginalId = sosc.SoftwareOptionSpecificationCodeId,
                        SpecCodeDefinitionId = sosc.SpecCodeDefinitionId,
                        Category = sosc.SpecCodeDefinition?.Category ?? string.Empty,
                        SpecCodeNo = sosc.SpecCodeDefinition?.SpecCodeNo ?? string.Empty,
                        SpecCodeBit = sosc.SpecCodeDefinition?.SpecCodeBit ?? string.Empty,
                        Description = sosc.SpecCodeDefinition?.Description,
                        IsDescriptionReadOnly = (sosc.SpecCodeDefinitionId > 0), // Readonly if loaded from existing
                        SoftwareOptionActivationRuleId = sosc.SoftwareOptionActivationRuleId,
                        SpecificInterpretation = sosc.SpecificInterpretation,
                        ActivationRuleName = AvailableActivationRules.FirstOrDefault(ar => ar.Id == sosc.SoftwareOptionActivationRuleId)?.DisplayName
                                             ?? sosc.SoftwareOptionActivationRule?.RuleName, // Fallback
                        ItemChangedCallback = MarkSpecCodesAsModified
                    };
                    vm.SpecCodeDisplayName = $"{vm.Category} - {vm.SpecCodeNo}/{vm.SpecCodeBit}"; // Update display name
                    SpecificationCodes.Add(vm);
                });

                ActivationRules.Clear();
                _originalSoftwareOption.SoftwareOptionActivationRules?.ToList().ForEach(ar => ActivationRules.Add(new ActivationRuleViewModel { OriginalId = ar.SoftwareOptionActivationRuleId, RuleName = ar.RuleName, ActivationSetting = ar.ActivationSetting, Notes = ar.Notes, ItemChangedCallback = MarkActivationRulesAsModified }));

                ResetDirtyFlags();
                OnPropertyChanged(nameof(ViewTitle));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading Software Option: {ex.ToString()}");
                PrimaryName = "ERROR: Load Failed"; OnPropertyChanged(nameof(ViewTitle));
            }
            finally { IsLoading = false; }
        }

        private void ExecuteShowSpecCodeDialogForAdd()
        {
            if (!ControlSystemId.HasValue || ControlSystemId.Value <= 0)
            {
                MessageBox.Show("Please select a Control System for the Software Option first.", "Control System Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var newSpecCodeInstance = new SpecCodeViewModel { IsDescriptionReadOnly = false };
            var dialogViewModel = new EditSpecCodeDialogViewModel(
        _softwareOptionService,
        newSpecCodeInstance, // The actual SpecCodeViewModel being added
        ControlSystemId.Value,
        AvailableActivationRules
    );

            // Now call ShowSpecCodeDialog with the dialogViewModel
            ShowSpecCodeDialog("Add Specification Code", dialogViewModel, true);
        }

        private void ExecuteShowSpecCodeDialogForEdit(SpecCodeViewModel? specCodeToEdit)
        {
            if (specCodeToEdit == null) return;
            if (!ControlSystemId.HasValue || ControlSystemId.Value <= 0)
            {
                MessageBox.Show("The parent Software Option's Control System is not set.", "Control System Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var dialogViewModel = new EditSpecCodeDialogViewModel(
        _softwareOptionService,
        specCodeToEdit, // The actual SpecCodeViewModel being edited
        ControlSystemId.Value,
        AvailableActivationRules
    );

            // Now call ShowSpecCodeDialog with the dialogViewModel
            ShowSpecCodeDialog("Edit Specification Code", dialogViewModel, false);
        }

        private void ShowSpecCodeDialog(string title, EditSpecCodeDialogViewModel dialogViewModel, bool isAddingNew)
        {
            // Use the _scopeFactory to create a new scope just for resolving the dialog view
            using (var dialogScope = _scopeFactory.CreateScope())
            {
                var dialogView = dialogScope.ServiceProvider.GetRequiredService<Views.EditSpecCodeDialog>();
                dialogView.DataContext = dialogViewModel;

                var dialogWindow = new Window
                {
                    Title = title,
                    Content = dialogView,
                    Width = 480,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current?.MainWindow,
                    ShowInTaskbar = false,
                    ResizeMode = ResizeMode.NoResize
                };

                SpecCodeViewModel? finalItem = null;
                Action<SpecCodeViewModel?>? dialogClosedHandler = null;
                dialogClosedHandler = (itemFromDialog) =>
                {
                    finalItem = itemFromDialog;
                    if (dialogViewModel != null && dialogClosedHandler != null)
                    {
                        dialogViewModel.DialogClosed -= dialogClosedHandler;
                    }
                    // The window itself should be closed by its ViewModel setting DialogResult,
                    // or by its own OK/Cancel buttons.
                    // This handler is primarily for processing the returned data.
                };
                dialogViewModel.DialogClosed += dialogClosedHandler;

                // In EditSpecCodeDialogViewModel's Save/Cancel commands:
                // Save: DialogClosed?.Invoke(SpecCodeToEdit); this.CloseDialogWindowAction?.Invoke(true);
                // Cancel: DialogClosed?.Invoke(null); this.CloseDialogWindowAction?.Invoke(false);
                // Add: public Action<bool?> CloseDialogWindowAction { get; set; } to EditSpecCodeDialogViewModel
                // Then set it here:
                dialogViewModel.CloseDialogWindowAction = (result) => {
                    dialogWindow.DialogResult = result;
                    dialogWindow.Close();
                };


                bool? resultState = dialogWindow.ShowDialog(); // This is blocking

                if (resultState == true && finalItem != null)
                {
                    finalItem.ItemChangedCallback = MarkSpecCodesAsModified; // Make sure this callback exists
                    if (isAddingNew)
                    {
                        SpecificationCodes.Add(finalItem);
                        SelectedSpecificationCode = finalItem;
                    }
                    else
                    {
                        // Item was edited by reference.
                        // If SpecCodeViewModel properties raise PropertyChanged, DataGrid should update.
                    }
                    MarkSpecCodesAsModified();
                }
            }
        }

        // --- (Rest of the methods: ExecuteRemoveSpecificationCode, ResetDirtyFlags, ExecuteSaveAsync, 
        //      OptionNumber and Requirement handlers are assumed to be mostly correct from your previous file) ---
        // Make sure ExecuteSaveAsync correctly maps the new SpecCodeViewModel properties to SoftwareOptionSpecificationCodeCreateDto

        private void ExecuteRemoveSpecificationCode()
        {
            if (SelectedSpecificationCode != null)
            {
                SpecificationCodes.Remove(SelectedSpecificationCode);
                MarkSpecCodesAsModified();
            }
        }
        private void SubscribeToCollectionChanges()
        {
            OptionNumbers.CollectionChanged += (s, e) => MarkCollectionAsModified(ref _isOptionNumbersModified);
            Requirements.CollectionChanged += (s, e) => MarkCollectionAsModified(ref _isRequirementsModified);
            SpecificationCodes.CollectionChanged += (s, e) => MarkCollectionAsModified(ref _isSpecCodesModified);
            ActivationRules.CollectionChanged += (s, e) => MarkCollectionAsModified(ref _isActivationRulesModified);
        }

        private void MarkScalarAsModified() => _isScalarModified = true;
        private void MarkCollectionAsModified(ref bool flag) => flag = true;
        private void MarkOptionNumbersAsModified() => MarkCollectionAsModified(ref _isOptionNumbersModified);
        private void MarkRequirementsAsModified() => MarkCollectionAsModified(ref _isRequirementsModified);
        private void MarkSpecCodesAsModified() => MarkCollectionAsModified(ref _isSpecCodesModified);
        private void MarkActivationRulesAsModified() => MarkCollectionAsModified(ref _isActivationRulesModified);

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
                MessageBox.Show("Primary Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (!ControlSystemId.HasValue || ControlSystemId.Value <= 0)
            {
                MessageBox.Show("Control System is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            IsLoading = true;
            string currentUser = _authStateProvider.CurrentUser?.UserName ?? "System";

            try
            {
                var specCodeCreateDtos = this.SpecificationCodes.Select(vm => new SoftwareOptionSpecificationCodeCreateDto
                {
                    Category = vm.Category, // From SpecCodeViewModel
                    SpecCodeNo = vm.SpecCodeNo, // From SpecCodeViewModel
                    SpecCodeBit = vm.SpecCodeBit, // From SpecCodeViewModel
                    Description = vm.Description, // From SpecCodeViewModel
                    SoftwareOptionActivationRuleId = vm.SoftwareOptionActivationRuleId,
                    SpecificInterpretation = vm.SpecificInterpretation
                    // The service will handle finding/creating SpecCodeDefinition based on Category, No, Bit, Desc and ControlSystemId
                }).ToList();

                if (_isNewSoftwareOption)
                {
                    var createDto = new CreateSoftwareOptionCommandDto
                    {
                        PrimaryName = this.PrimaryName,
                        AlternativeNames = this.AlternativeNames,
                        SourceFileName = this.SourceFileName,
                        PrimaryOptionNumberDisplay = this.PrimaryOptionNumberDisplay,
                        Notes = this.Notes,
                        ControlSystemId = this.ControlSystemId, // Ensured not null by check above
                        OptionNumbers = this.OptionNumbers.Select(vm => new OptionNumberRegistryCreateDto { OptionNumber = vm.OptionNumber }).ToList(),
                        Requirements = this.Requirements.Select(vm => new RequirementCreateDto
                        {
                            RequirementType = vm.RequirementType,
                            Condition = vm.Condition,
                            GeneralRequiredValue = vm.GeneralRequiredValue,
                            RequiredSoftwareOptionId = vm.RequiredSoftwareOptionId,
                            RequiredSpecCodeDefinitionId = vm.RequiredSpecCodeDefinitionId,
                            OspFileName = vm.OspFileName,
                            OspFileVersion = vm.OspFileVersion,
                            Notes = vm.Notes
                        }).ToList(),
                        SpecificationCodes = specCodeCreateDtos, // Use the new mapping
                        ActivationRules = this.ActivationRules.Select(vm => new SoftwareOptionActivationRuleCreateDto
                        {
                            RuleName = vm.RuleName,
                            ActivationSetting = vm.ActivationSetting,
                            Notes = vm.Notes
                        }).ToList()
                    };

                    var createdOption = await _softwareOptionService.CreateSoftwareOptionAsync(createDto, currentUser);
                    if (createdOption != null)
                    {
                        await LoadSoftwareOptionAsync(createdOption.SoftwareOptionId);
                        _isNewSoftwareOption = false;
                        ResetDirtyFlags();
                        IsLoading = false;
                        return true;
                    }
                    else { /* Handle create failure */ IsLoading = false; return false; }
                }
                else
                {
                    if (_originalSoftwareOption == null) { MessageBox.Show("Original option data is missing.", "Error"); IsLoading = false; return false; }
                    if (!_isScalarModified && !_isOptionNumbersModified && !_isRequirementsModified && !_isSpecCodesModified && !_isActivationRulesModified)
                    {
                        IsLoading = false; return true;
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
                        ControlSystemId = this.ControlSystemId, // Ensured not null by check above
                        OptionNumbers = _isOptionNumbersModified ? this.OptionNumbers.Select(vm => new OptionNumberRegistryCreateDto { OptionNumber = vm.OptionNumber }).ToList() : null,
                        Requirements = _isRequirementsModified ? this.Requirements.Select(vm => new RequirementCreateDto
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
                        SpecificationCodes = _isSpecCodesModified ? specCodeCreateDtos : null, // Use new mapping
                        ActivationRules = _isActivationRulesModified ? this.ActivationRules.Select(vm => new SoftwareOptionActivationRuleCreateDto
                        {
                            RuleName = vm.RuleName,
                            ActivationSetting = vm.ActivationSetting,
                            Notes = vm.Notes
                        }).ToList() : null
                    };

                    var updatedOption = await _softwareOptionService.UpdateSoftwareOptionAsync(updateDto, currentUser);
                    if (updatedOption != null)
                    {
                        await LoadSoftwareOptionAsync(this.SoftwareOptionId);
                        IsLoading = false;
                        return true;
                    }
                    else { /* Handle update failure */ IsLoading = false; return false; }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving Software Option: {ex.ToString()}");
                MessageBox.Show($"Error saving: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                IsLoading = false;
                return false;
            }
        }
        private void ExecuteAddOptionNumber()
        {
            var newOptionNumber = new OptionNumberViewModel { OptionNumber = "New #" };
            newOptionNumber.ItemChangedCallback = MarkOptionNumbersAsModified;
            OptionNumbers.Add(newOptionNumber);
            MarkOptionNumbersAsModified();
        }

        private void ExecuteRemoveOptionNumber()
        {
            if (SelectedOptionNumber != null)
            {
                OptionNumbers.Remove(SelectedOptionNumber);
                MarkOptionNumbersAsModified();
            }
        }
        private void ExecuteAddRequirement()
        {
            var newReqVm = new RequirementViewModel();
            newReqVm.ItemChangedCallback = MarkRequirementsAsModified;
            Requirements.Add(newReqVm);
            MarkRequirementsAsModified();
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