// File: RuleArchitect.DesktopClient/ViewModels/EditSoftwareOptionViewModel.cs
using GenesisSentry.Interfaces;
using HeraldKit.Interfaces; // For INotificationService
using RuleArchitect.ApplicationLogic.DTOs;
using RuleArchitect.ApplicationLogic.Interfaces;
using RuleArchitect.DesktopClient.Commands;
using RuleArchitect.DesktopClient.Views; // For EditSpecCodeDialog
using RuleArchitect.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection; // For IServiceScopeFactory, IServiceProvider
using Microsoft.EntityFrameworkCore; // For DbUpdateException

namespace RuleArchitect.DesktopClient.ViewModels
{
    // Simple DTOs for lookup collections if not already defined elsewhere
    // You might already have suitable DTOs in ApplicationLogic.DTOs
    public class SoftwareOptionLookupDto
    {
        public int SoftwareOptionId { get; set; }
        public string? PrimaryName { get; set; }
        // Add ControlSystemId if needed for filtering within RequirementViewModel or service calls
        public int? ControlSystemId { get; set; }
    }

    public class SpecCodeDefinitionLookupDto
    {
        public int SpecCodeDefinitionId { get; set; }
        public string? DisplayName { get; set; } // e.g., "NC1 - S001/B01: Description"
        public int ControlSystemId { get; set; } // To ensure it matches the SO's CS
    }


    public class EditSoftwareOptionViewModel : BaseViewModel
    {
        private readonly IAuthenticationStateProvider _authStateProvider;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly INotificationService _notificationService;

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

        // *** NEW: Lookup collections for Requirements ***
        public ObservableCollection<SoftwareOptionLookupDto> AvailableSoftwareOptionsForRequirements { get; }
        public ObservableCollection<SpecCodeDefinitionLookupDto> AvailableSpecCodesForRequirements { get; }
        // *** END NEW ***


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
                    // When ControlSystemId changes, we might need to reload/filter
                    // AvailableSpecCodesForRequirements if they are control system specific.
                    _ = LoadSpecCodeDefinitionsForRequirementsAsync();
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
                    ((RelayCommand)EditSpecificationCodeCommand).RaiseCanExecuteChanged();
                }
            }
        }

        // *** NEW: SelectedItem for Requirements (if needed for an Edit button outside ItemsControl) ***
        private RequirementViewModel? _selectedRequirement;
        public RequirementViewModel? SelectedRequirement
        {
            get => _selectedRequirement;
            set => SetProperty(ref _selectedRequirement, value, () =>
            {
                ((RelayCommand)RemoveRequirementCommand).RaiseCanExecuteChanged();
                // If you add an EditRequirementCommand that operates on SelectedRequirement:
                // ((RelayCommand)EditRequirementCommand).RaiseCanExecuteChanged();
            });
        }
        // *** END NEW ***

        public ICommand AddSpecificationCodeCommand { get; }
        public ICommand EditSpecificationCodeCommand { get; }
        public ICommand RemoveSpecificationCodeCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand AddOptionNumberCommand { get; }
        public ICommand RemoveOptionNumberCommand { get; }
        public ICommand AddRequirementCommand { get; }
        public ICommand RemoveRequirementCommand { get; }

        private OptionNumberViewModel? _selectedOptionNumber;
        public OptionNumberViewModel? SelectedOptionNumber { get => _selectedOptionNumber; set => SetProperty(ref _selectedOptionNumber, value, () => ((RelayCommand)RemoveOptionNumberCommand).RaiseCanExecuteChanged()); }


        public EditSoftwareOptionViewModel() // Parameterless for Designer
        {
            _authStateProvider = null!;
            _scopeFactory = null!;
            _notificationService = null!;

            OptionNumbers = new ObservableCollection<OptionNumberViewModel>();
            Requirements = new ObservableCollection<RequirementViewModel>();
            SpecificationCodes = new ObservableCollection<SpecCodeViewModel>();
            ActivationRules = new ObservableCollection<ActivationRuleViewModel>();
            AvailableControlSystems = new ObservableCollection<ControlSystemLookupDto>();
            AvailableActivationRules = new ObservableCollection<ActivationRuleLookupDto>();

            // *** NEW: Initialize new collections ***
            AvailableSoftwareOptionsForRequirements = new ObservableCollection<SoftwareOptionLookupDto>();
            AvailableSpecCodesForRequirements = new ObservableCollection<SpecCodeDefinitionLookupDto>();
            // *** END NEW ***

            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                PrimaryName = "Sample Software Option (Design)";
                AvailableControlSystems.Add(new ControlSystemLookupDto { ControlSystemId = 1, Name = "Design CS" });
                AvailableActivationRules.Add(new ActivationRuleLookupDto { Id = 1, DisplayName = "Design Rule" });
                SpecificationCodes.Add(new SpecCodeViewModel { Category = "D_CAT", SpecCodeNo = "D_S001", SpecCodeBit = "D_B01" });
                // Add design-time data for new lookups
                AvailableSoftwareOptionsForRequirements.Add(new SoftwareOptionLookupDto { SoftwareOptionId = 1, PrimaryName = "Design SO Req" });
                AvailableSpecCodesForRequirements.Add(new SpecCodeDefinitionLookupDto { SpecCodeDefinitionId = 1, DisplayName = "Design SpecCode Req" });
                Requirements.Add(new RequirementViewModel { RequirementType = "General Text", GeneralRequiredValue = "Design time requirement" });

            }
        }

        public EditSoftwareOptionViewModel(
            IAuthenticationStateProvider authStateProvider,
            IServiceScopeFactory scopeFactory,
            INotificationService notificationService)
        {
            _authStateProvider = authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

            _isNewSoftwareOption = true;
            OptionNumbers = new ObservableCollection<OptionNumberViewModel>();
            Requirements = new ObservableCollection<RequirementViewModel>();
            SpecificationCodes = new ObservableCollection<SpecCodeViewModel>();
            ActivationRules = new ObservableCollection<ActivationRuleViewModel>();
            AvailableControlSystems = new ObservableCollection<ControlSystemLookupDto>();
            AvailableActivationRules = new ObservableCollection<ActivationRuleLookupDto>();

            // *** NEW: Initialize new collections ***
            AvailableSoftwareOptionsForRequirements = new ObservableCollection<SoftwareOptionLookupDto>();
            AvailableSpecCodesForRequirements = new ObservableCollection<SpecCodeDefinitionLookupDto>();
            // *** END NEW ***

            SubscribeToCollectionChanges();
            SaveCommand = new RelayCommand(async () => await ExecuteSaveAsync(), () => !IsLoading);
            AddOptionNumberCommand = new RelayCommand(ExecuteAddOptionNumber);
            RemoveOptionNumberCommand = new RelayCommand(ExecuteRemoveOptionNumber, () => SelectedOptionNumber != null);

            AddRequirementCommand = new RelayCommand(ExecuteAddRequirement);
            RemoveRequirementCommand = new RelayCommand(ExecuteRemoveRequirement, () => SelectedRequirement != null); // Now uses SelectedRequirement

            AddSpecificationCodeCommand = new RelayCommand(ExecuteShowSpecCodeDialogForAdd);
            EditSpecificationCodeCommand = new RelayCommand(param => ExecuteShowSpecCodeDialogForEdit(param as SpecCodeViewModel), param => param is SpecCodeViewModel);
            RemoveSpecificationCodeCommand = new RelayCommand(ExecuteRemoveSpecificationCode, () => SelectedSpecificationCode != null);

            PrimaryName = "";
            Version = 1;
            LastModifiedDate = DateTime.UtcNow;
            LastModifiedBy = _authStateProvider.CurrentUser?.UserName ?? "System";
            ResetDirtyFlags();
            OnPropertyChanged(nameof(ViewTitle));
            _ = LoadInitialLookupsAsync(); // Keep this
        }

        public class ActivationRuleLookupDto { public int Id { get; set; } public string? DisplayName { get; set; } }

        private async Task LoadInitialLookupsAsync()
        {
            IsLoading = true;
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var softwareOptionService = scope.ServiceProvider.GetRequiredService<ISoftwareOptionService>();

                    // Load Control Systems
                    var controlSystems = await softwareOptionService.GetControlSystemLookupsAsync();
                    Application.Current.Dispatcher.Invoke(() => {
                        AvailableControlSystems.Clear();
                        if (controlSystems != null) foreach (var cs in controlSystems) AvailableControlSystems.Add(cs);
                        OnPropertyChanged(nameof(AvailableControlSystems));
                    });

                    // *** NEW: Load Software Options for Requirements lookup ***
                    var allSoftwareOptions = await softwareOptionService.GetAllSoftwareOptionsAsync();
                    Application.Current.Dispatcher.Invoke(() => {
                        AvailableSoftwareOptionsForRequirements.Clear();
                        if (allSoftwareOptions != null)
                        {
                            foreach (var so in allSoftwareOptions.OrderBy(s => s.PrimaryName))
                            {
                                AvailableSoftwareOptionsForRequirements.Add(new SoftwareOptionLookupDto
                                {
                                    SoftwareOptionId = so.SoftwareOptionId,
                                    PrimaryName = so.PrimaryName,
                                    ControlSystemId = so.ControlSystemId // Include if useful for filtering
                                });
                            }
                        }
                        OnPropertyChanged(nameof(AvailableSoftwareOptionsForRequirements));
                    });
                    // *** END NEW ***

                    // Load Spec Code Definitions (will be filtered by ControlSystemId later if needed)
                    // This is now handled by LoadSpecCodeDefinitionsForRequirementsAsync, called when ControlSystemId changes.
                    // However, you might want an initial load if ControlSystemId is already set.
                    await LoadSpecCodeDefinitionsForRequirementsAsync();


                    // Dummy Activation Rules (replace with actual service call if available)
                    if (!AvailableActivationRules.Any())
                    {
                        AvailableActivationRules.Add(new ActivationRuleLookupDto { Id = 1, DisplayName = "AR001 - Test Rule 1" });
                        AvailableActivationRules.Add(new ActivationRuleLookupDto { Id = 2, DisplayName = "AR002 - Test Rule 2" });
                        OnPropertyChanged(nameof(AvailableActivationRules));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading initial lookups: {ex.Message}");
                _notificationService.ShowError($"Error loading lookups: {ex.Message}", "Load Error");
            }
            finally { IsLoading = false; }
        }

        // *** NEW: Method to load/filter SpecCodeDefinitions for requirements based on ControlSystemId ***
        private async Task LoadSpecCodeDefinitionsForRequirementsAsync()
        {
            if (!ControlSystemId.HasValue || ControlSystemId.Value <= 0)
            {
                Application.Current.Dispatcher.Invoke(() => {
                    AvailableSpecCodesForRequirements.Clear();
                    OnPropertyChanged(nameof(AvailableSpecCodesForRequirements));
                });
                return;
            }

            // Consider adding a loading indicator specific to this part if it's slow
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var softwareOptionService = scope.ServiceProvider.GetRequiredService<ISoftwareOptionService>();
                    var specCodes = await softwareOptionService.GetSpecCodeDefinitionsForControlSystemAsync(ControlSystemId.Value);

                    Application.Current.Dispatcher.Invoke(() => {
                        AvailableSpecCodesForRequirements.Clear();
                        if (specCodes != null)
                        {
                            foreach (var scd in specCodes.OrderBy(s => s.Category).ThenBy(s => s.SpecCodeNo).ThenBy(s => s.SpecCodeBit))
                            {
                                AvailableSpecCodesForRequirements.Add(new SpecCodeDefinitionLookupDto
                                {
                                    SpecCodeDefinitionId = scd.SpecCodeDefinitionId,
                                    DisplayName = $"{scd.Category} - {scd.SpecCodeNo}/{scd.SpecCodeBit}: {scd.Description?.Substring(0, Math.Min(scd.Description.Length, 30)) ?? ""}{(scd.Description?.Length > 30 ? "..." : "")}",
                                    ControlSystemId = scd.ControlSystemId
                                });
                            }
                        }
                        OnPropertyChanged(nameof(AvailableSpecCodesForRequirements));
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading spec code definitions for requirements: {ex.Message}");
                _notificationService.ShowError($"Error loading spec code definitions for requirements: {ex.Message}", "Load Error");
                Application.Current.Dispatcher.Invoke(() => {
                    AvailableSpecCodesForRequirements.Clear();
                    OnPropertyChanged(nameof(AvailableSpecCodesForRequirements));
                });
            }
        }
        // *** END NEW ***


        public async Task LoadSoftwareOptionAsync(int softwareOptionIdToLoad)
        {
            IsLoading = true;
            _isNewSoftwareOption = false;

            // Ensure base lookups are loaded first, especially Control Systems
            if (!AvailableControlSystems.Any() || !AvailableSoftwareOptionsForRequirements.Any())
            {
                await LoadInitialLookupsAsync(); // This will also call LoadSpecCodeDefinitionsForRequirementsAsync if CS ID is set
            }

            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var softwareOptionService = scope.ServiceProvider.GetRequiredService<ISoftwareOptionService>();
                    _originalSoftwareOption = await softwareOptionService.GetSoftwareOptionByIdAsync(softwareOptionIdToLoad);
                }

                if (_originalSoftwareOption == null)
                {
                    _notificationService.ShowError($"Software Option with ID {softwareOptionIdToLoad} not found.", "Load Error");
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
                // Setting ControlSystemId here will trigger LoadSpecCodeDefinitionsForRequirementsAsync
                ControlSystemId = _originalSoftwareOption.ControlSystemId;
                Version = _originalSoftwareOption.Version;
                LastModifiedDate = _originalSoftwareOption.LastModifiedDate;
                LastModifiedBy = _originalSoftwareOption.LastModifiedBy;

                OptionNumbers.Clear();
                _originalSoftwareOption.OptionNumberRegistries?.ToList().ForEach(onr => OptionNumbers.Add(new OptionNumberViewModel { OriginalId = onr.OptionNumberRegistryId, OptionNumber = onr.OptionNumber, ItemChangedCallback = MarkOptionNumbersAsModified }));

                Requirements.Clear();
                if (_originalSoftwareOption.Requirements != null)
                {
                    foreach (var r in _originalSoftwareOption.Requirements)
                    {
                        var reqVm = new RequirementViewModel
                        {
                            OriginalId = r.RequirementId,
                            RequirementType = r.RequirementType,
                            Condition = r.Condition,
                            GeneralRequiredValue = r.GeneralRequiredValue ?? string.Empty,
                            RequiredSoftwareOptionId = r.RequiredSoftwareOptionId,
                            RequiredSpecCodeDefinitionId = r.RequiredSpecCodeDefinitionId,
                            OspFileName = r.OspFileName,
                            OspFileVersion = r.OspFileVersion,
                            Notes = r.Notes,
                            ItemChangedCallback = MarkRequirementsAsModified
                        };
                        // Populate display names
                        if (r.RequiredSoftwareOptionId.HasValue)
                        {
                            reqVm.RequiredSoftwareOptionName = AvailableSoftwareOptionsForRequirements
                                .FirstOrDefault(so => so.SoftwareOptionId == r.RequiredSoftwareOptionId.Value)?.PrimaryName;
                        }
                        if (r.RequiredSpecCodeDefinitionId.HasValue)
                        {
                            reqVm.RequiredSpecCodeName = AvailableSpecCodesForRequirements
                               .FirstOrDefault(sc => sc.SpecCodeDefinitionId == r.RequiredSpecCodeDefinitionId.Value)?.DisplayName;
                        }
                        Requirements.Add(reqVm);
                    }
                }

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
                        IsDescriptionReadOnly = (sosc.SpecCodeDefinitionId > 0),
                        SoftwareOptionActivationRuleId = sosc.SoftwareOptionActivationRuleId,
                        SpecificInterpretation = sosc.SpecificInterpretation,
                        ActivationRuleName = AvailableActivationRules.FirstOrDefault(ar => ar.Id == sosc.SoftwareOptionActivationRuleId)?.DisplayName ?? sosc.SoftwareOptionActivationRule?.RuleName,
                        ItemChangedCallback = MarkSpecCodesAsModified
                    };
                    vm.SpecCodeDisplayName = $"{vm.Category} - {vm.SpecCodeNo}/{vm.SpecCodeBit}";
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
                _notificationService.ShowError($"Error loading option: {ex.Message}", "Load Error");
                PrimaryName = "ERROR: Load Failed"; OnPropertyChanged(nameof(ViewTitle));
            }
            finally { IsLoading = false; }
        }

        private void ExecuteShowSpecCodeDialogForAdd()
        {
            if (!ControlSystemId.HasValue || ControlSystemId.Value <= 0)
            {
                _notificationService.ShowWarning("Please select a Control System for the Software Option first.", "Control System Required");
                return;
            }
            string currentControlSystemName = AvailableControlSystems.FirstOrDefault(cs => cs.ControlSystemId == ControlSystemId.Value)?.Name ?? "Unknown";
            var newSpecCodeInstance = new SpecCodeViewModel { IsDescriptionReadOnly = false };

            var dialogViewModel = new EditSpecCodeDialogViewModel(
                _scopeFactory,
                newSpecCodeInstance,
                currentControlSystemName,
                ControlSystemId.Value,
                AvailableActivationRules
            );
            ShowSpecCodeDialog("Add Specification Code", dialogViewModel, true);
        }

        private void ExecuteShowSpecCodeDialogForEdit(SpecCodeViewModel? specCodeToEdit)
        {
            if (specCodeToEdit == null) return;
            if (!ControlSystemId.HasValue || ControlSystemId.Value <= 0)
            {
                _notificationService.ShowWarning("The parent Software Option's Control System is not set.", "Control System Required");
                return;
            }
            string currentControlSystemName = AvailableControlSystems.FirstOrDefault(cs => cs.ControlSystemId == ControlSystemId.Value)?.Name ?? "Unknown";

            var dialogViewModel = new EditSpecCodeDialogViewModel(
                _scopeFactory,
                specCodeToEdit,
                currentControlSystemName,
                ControlSystemId.Value,
                AvailableActivationRules
            );
            ShowSpecCodeDialog("Edit Specification Code", dialogViewModel, false);
        }

        private void ShowSpecCodeDialog(string title, EditSpecCodeDialogViewModel dialogViewModel, bool isAddingNew)
        {
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
                dialogClosedHandler = (itemFromDialog) => {
                    finalItem = itemFromDialog;
                    if (dialogViewModel != null && dialogClosedHandler != null)
                        dialogViewModel.DialogClosed -= dialogClosedHandler;
                };
                dialogViewModel.DialogClosed += dialogClosedHandler;

                dialogViewModel.CloseDialogWindowAction = (result) => {
                    dialogWindow.DialogResult = result;
                    dialogWindow.Close();
                };

                bool? resultState = dialogWindow.ShowDialog();

                if (resultState == true && finalItem != null)
                {
                    finalItem.ItemChangedCallback = MarkSpecCodesAsModified;
                    if (isAddingNew)
                    {
                        SpecificationCodes.Add(finalItem);
                        SelectedSpecificationCode = finalItem;
                    }
                    else // Editing existing, ensure UI updates if properties were changed
                    {
                        // The finalItem IS the instance from the collection, so direct changes
                        // in the dialog should already be reflected if bound two-way.
                        // If not, or if a copy was edited, replace item here.
                        // Forcing a refresh if needed:
                        var existingItem = SpecificationCodes.FirstOrDefault(sc => sc.OriginalId == finalItem.OriginalId && finalItem.OriginalId != 0);
                        if (existingItem != null && existingItem != finalItem)
                        {
                            int index = SpecificationCodes.IndexOf(existingItem);
                            SpecificationCodes[index] = finalItem; // Replace
                        }
                        else if (existingItem == null && !isAddingNew && finalItem.OriginalId != 0)
                        {
                            //This case should ideally not happen if editing an existing item.
                            //It means the item was removed from the list while dialog was open.
                        }
                    }
                    MarkSpecCodesAsModified();
                }
            }
        }

        public async Task<bool> ExecuteSaveAsync()
        {
            if (string.IsNullOrWhiteSpace(PrimaryName))
            { _notificationService.ShowWarning("Primary Name is required.", "Validation Error"); return false; }
            if (!ControlSystemId.HasValue || ControlSystemId.Value <= 0)
            { _notificationService.ShowWarning("Control System is required.", "Validation Error"); return false; }

            IsLoading = true;
            string currentUser = _authStateProvider.CurrentUser?.UserName ?? "System";
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var softwareOptionService = scope.ServiceProvider.GetRequiredService<ISoftwareOptionService>();
                    var specCodeCreateDtos = this.SpecificationCodes.Select(vm => new SoftwareOptionSpecificationCodeCreateDto
                    {
                        Category = vm.Category,
                        SpecCodeNo = vm.SpecCodeNo,
                        SpecCodeBit = vm.SpecCodeBit,
                        Description = vm.Description, // This will be used if SpecCodeDefinition is new
                        SoftwareOptionActivationRuleId = vm.SoftwareOptionActivationRuleId,
                        SpecificInterpretation = vm.SpecificInterpretation
                        // Note: SpecCodeDefinitionId is resolved by the service based on Category,No,Bit,ControlSystem
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
                            ControlSystemId = this.ControlSystemId,
                            OptionNumbers = this.OptionNumbers.Select(vm => new OptionNumberRegistryCreateDto { OptionNumber = vm.OptionNumber }).ToList(),
                            Requirements = this.Requirements.Select(vm => new RequirementCreateDto { RequirementType = vm.RequirementType, Condition = vm.Condition, GeneralRequiredValue = vm.GeneralRequiredValue ?? string.Empty, RequiredSoftwareOptionId = vm.RequiredSoftwareOptionId, RequiredSpecCodeDefinitionId = vm.RequiredSpecCodeDefinitionId, OspFileName = vm.OspFileName, OspFileVersion = vm.OspFileVersion, Notes = vm.Notes }).ToList(),
                            SpecificationCodes = specCodeCreateDtos,
                            ActivationRules = this.ActivationRules.Select(vm => new SoftwareOptionActivationRuleCreateDto { RuleName = vm.RuleName, ActivationSetting = vm.ActivationSetting, Notes = vm.Notes }).ToList()
                        };
                        var createdOption = await softwareOptionService.CreateSoftwareOptionAsync(createDto, currentUser);
                        if (createdOption != null)
                        {
                            await LoadSoftwareOptionAsync(createdOption.SoftwareOptionId);
                            _isNewSoftwareOption = false; ResetDirtyFlags();
                            _notificationService.ShowSuccess($"Software Option '{createdOption.PrimaryName}' created successfully.", "Save Successful");
                            IsLoading = false; return true;
                        }
                        else { _notificationService.ShowError("Failed to create Software Option.", "Save Failed"); IsLoading = false; return false; }
                    }
                    else
                    {
                        if (_originalSoftwareOption == null) { _notificationService.ShowError("Original option data is missing for update.", "Save Error"); IsLoading = false; return false; }
                        if (!_isScalarModified && !_isOptionNumbersModified && !_isRequirementsModified && !_isSpecCodesModified && !_isActivationRulesModified)
                        { _notificationService.ShowInformation("No changes detected to save.", "Save Information"); IsLoading = false; return true; }

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
                            Requirements = _isRequirementsModified ? this.Requirements.Select(vm => new RequirementCreateDto { RequirementType = vm.RequirementType, Condition = vm.Condition, GeneralRequiredValue = vm.GeneralRequiredValue ?? string.Empty, RequiredSoftwareOptionId = vm.RequiredSoftwareOptionId, RequiredSpecCodeDefinitionId = vm.RequiredSpecCodeDefinitionId, OspFileName = vm.OspFileName, OspFileVersion = vm.OspFileVersion, Notes = vm.Notes }).ToList() : null,
                            SpecificationCodes = _isSpecCodesModified ? specCodeCreateDtos : null,
                            ActivationRules = _isActivationRulesModified ? this.ActivationRules.Select(vm => new SoftwareOptionActivationRuleCreateDto { RuleName = vm.RuleName, ActivationSetting = vm.ActivationSetting, Notes = vm.Notes }).ToList() : null
                        };
                        var updatedOption = await softwareOptionService.UpdateSoftwareOptionAsync(updateDto, currentUser);
                        if (updatedOption != null)
                        {
                            await LoadSoftwareOptionAsync(this.SoftwareOptionId); // Reload to get fresh data and reset dirty flags via Load
                            _notificationService.ShowSuccess($"Software Option '{updatedOption.PrimaryName}' updated successfully.", "Save Successful");
                            IsLoading = false; return true;
                        }
                        else { _notificationService.ShowError("Failed to update Software Option.", "Save Failed"); IsLoading = false; return false; }
                    }
                }
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"Database error saving Software Option: {dbEx.ToString()}");
                if (dbEx.InnerException is Microsoft.Data.Sqlite.SqliteException sqliteEx && sqliteEx.SqliteErrorCode == 19) // Specific for SQLite unique constraint
                {
                    _notificationService.ShowError($"Failed to save: A record with the same unique key (e.g., Spec Code No, Bit, Control System, Category) already exists. Details: {sqliteEx.Message}", "Save Error - Unique Constraint");
                }
                else
                {
                    _notificationService.ShowError($"Error saving to database: {dbEx.GetBaseException().Message}", "Database Save Error");
                }
                IsLoading = false; return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving Software Option: {ex.ToString()}");
                _notificationService.ShowError($"An unexpected error occurred while saving: {ex.Message}", "Save Error");
                IsLoading = false; return false;
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
        private void ExecuteAddOptionNumber()
        {
            var newOptionNumber = new OptionNumberViewModel { OptionNumber = "New #" };
            newOptionNumber.ItemChangedCallback = MarkOptionNumbersAsModified; // Ensure new items also trigger dirty flag
            OptionNumbers.Add(newOptionNumber);
            // MarkOptionNumbersAsModified(); // CollectionChanged already does this
        }

        private void ExecuteRemoveOptionNumber()
        {
            if (SelectedOptionNumber != null)
            {
                OptionNumbers.Remove(SelectedOptionNumber);
                // MarkOptionNumbersAsModified(); // CollectionChanged already does this
            }
        }
        private void ExecuteAddRequirement()
        {
            var newReqVm = new RequirementViewModel();
            // Set a default RequirementType if desired
            newReqVm.RequirementType = RequirementViewModel.AvailableRequirementTypes.FirstOrDefault() ?? string.Empty;
            newReqVm.ItemChangedCallback = MarkRequirementsAsModified; // Ensure new items also trigger dirty flag
            Requirements.Add(newReqVm);
            // MarkRequirementsAsModified(); // CollectionChanged already does this
        }

        private void ExecuteRemoveRequirement()
        {
            if (SelectedRequirement != null)
            {
                Requirements.Remove(SelectedRequirement);
                // MarkRequirementsAsModified(); // CollectionChanged already does this
            }
        }
        private void ExecuteRemoveSpecificationCode()
        {
            if (SelectedSpecificationCode != null)
            {
                SpecificationCodes.Remove(SelectedSpecificationCode);
                // MarkSpecCodesAsModified(); // CollectionChanged already does this
            }
        }
    }
}