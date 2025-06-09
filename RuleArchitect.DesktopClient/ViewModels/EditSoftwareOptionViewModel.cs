using GenesisSentry.Interfaces;
using HeraldKit.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RuleArchitect.Abstractions.DTOs;
using RuleArchitect.Abstractions.Interfaces;
using RuleArchitect.DesktopClient.Commands;
using RuleArchitect.DesktopClient.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RuleArchitect.DesktopClient.ViewModels
{
    public class EditSoftwareOptionViewModel : BaseViewModel
    {
        // NOTE: The local/nested DTO class definitions that were here have been removed.
        // This ViewModel now uses the official DTOs from the RuleArchitect.Abstractions project.

        private readonly IAuthenticationStateProvider _authStateProvider;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly INotificationService _notificationService;

        private int _softwareOptionId;
        private SoftwareOptionDetailDto? _originalSoftwareOptionDto;
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
        public ObservableCollection<SoftwareOptionLookupDto> AvailableSoftwareOptionsForRequirements { get; }
        public ObservableCollection<SpecCodeDefinitionLookupDto> AvailableSpecCodesForRequirements { get; }

        public string ViewTitle => _isNewSoftwareOption
                                   ? "Create New Software Option"
                                   : $"Edit Software Option (Name: {PrimaryName})";

        #region Public Properties
        public bool IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value, () => ((RelayCommand)SaveCommand).RaiseCanExecuteChanged()); }
        public string PrimaryName { get => _primaryName; set => SetProperty(ref _primaryName, value, onChanged: () => { MarkScalarAsModified(); OnPropertyChanged(nameof(ViewTitle)); }); }
        public string? AlternativeNames { get => _alternativeNames; set => SetProperty(ref _alternativeNames, value, MarkScalarAsModified); }
        public string? SourceFileName { get => _sourceFileName; set => SetProperty(ref _sourceFileName, value, MarkScalarAsModified); }
        public string? PrimaryOptionNumberDisplay { get => _primaryOptionNumberDisplay; set => SetProperty(ref _primaryOptionNumberDisplay, value, MarkScalarAsModified); }
        public string? Notes { get => _notes; set => SetProperty(ref _notes, value, MarkScalarAsModified); }
        public string? CheckedBy { get => _checkedBy; set => SetProperty(ref _checkedBy, value, MarkScalarAsModified); }
        public DateTime? CheckedDate { get => _checkedDate; set => SetProperty(ref _checkedDate, value, MarkScalarAsModified); }
        public int? ControlSystemId { get => _controlSystemId; set { if (SetProperty(ref _controlSystemId, value, MarkScalarAsModified)) { _ = LoadSpecCodeDefinitionsForRequirementsAsync(); } } }
        public int Version { get => _version; private set => SetProperty(ref _version, value); }
        public DateTime LastModifiedDate { get => _lastModifiedDate; private set => SetProperty(ref _lastModifiedDate, value); }
        public string? LastModifiedBy { get => _lastModifiedBy; private set => SetProperty(ref _lastModifiedBy, value); }
        public int SoftwareOptionId { get => _softwareOptionId; private set => SetProperty(ref _softwareOptionId, value, onChanged: () => OnPropertyChanged(nameof(ViewTitle))); }
        #endregion

        private SpecCodeViewModel? _selectedSpecificationCode;
        public SpecCodeViewModel? SelectedSpecificationCode { get => _selectedSpecificationCode; set { if (SetProperty(ref _selectedSpecificationCode, value)) { ((RelayCommand)RemoveSpecificationCodeCommand).RaiseCanExecuteChanged(); ((RelayCommand)EditSpecificationCodeCommand).RaiseCanExecuteChanged(); } } }

        private RequirementViewModel? _selectedRequirement;
        public RequirementViewModel? SelectedRequirement { get => _selectedRequirement; set => SetProperty(ref _selectedRequirement, value, () => { ((RelayCommand)RemoveRequirementCommand).RaiseCanExecuteChanged(); }); }

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

        // Parameterless constructor for XAML Designer
        public EditSoftwareOptionViewModel()
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
            AvailableSoftwareOptionsForRequirements = new ObservableCollection<SoftwareOptionLookupDto>();
            AvailableSpecCodesForRequirements = new ObservableCollection<SpecCodeDefinitionLookupDto>();

            // Design-time data
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                PrimaryName = "Sample Software Option (Design)";
                AvailableControlSystems.Add(new ControlSystemLookupDto { ControlSystemId = 1, Name = "Design CS" });
                AvailableActivationRules.Add(new ActivationRuleLookupDto { Id = 1, DisplayName = "Design Rule" });
                SpecificationCodes.Add(new SpecCodeViewModel { Category = "D_CAT", SpecCodeNo = "D_S001", SpecCodeBit = "D_B01" });
                AvailableSoftwareOptionsForRequirements.Add(new SoftwareOptionLookupDto { SoftwareOptionId = 1, PrimaryName = "Design SO Req" });
                AvailableSpecCodesForRequirements.Add(new SpecCodeDefinitionLookupDto { SpecCodeDefinitionId = 1, DisplayName = "Design SpecCode Req" });
                Requirements.Add(new RequirementViewModel { RequirementType = "General Text", GeneralRequiredValue = "Design time requirement" });
            }
        }

        public EditSoftwareOptionViewModel(IAuthenticationStateProvider authStateProvider, IServiceScopeFactory scopeFactory, INotificationService notificationService)
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
            AvailableSoftwareOptionsForRequirements = new ObservableCollection<SoftwareOptionLookupDto>();
            AvailableSpecCodesForRequirements = new ObservableCollection<SpecCodeDefinitionLookupDto>();

            SubscribeToCollectionChanges();
            SaveCommand = new RelayCommand(async () => await ExecuteSaveAsync(), () => !IsLoading);
            AddOptionNumberCommand = new RelayCommand(ExecuteAddOptionNumber);
            RemoveOptionNumberCommand = new RelayCommand(ExecuteRemoveOptionNumber, () => SelectedOptionNumber != null);
            AddRequirementCommand = new RelayCommand(ExecuteAddRequirement);
            RemoveRequirementCommand = new RelayCommand(ExecuteRemoveRequirement, () => SelectedRequirement != null);
            AddSpecificationCodeCommand = new RelayCommand(ExecuteShowSpecCodeDialogForAdd);
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

        public async Task LoadSoftwareOptionAsync(int softwareOptionIdToLoad)
        {
            IsLoading = true;
            _isNewSoftwareOption = false;
            if (!AvailableControlSystems.Any()) await LoadInitialLookupsAsync();

            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var softwareOptionService = scope.ServiceProvider.GetRequiredService<ISoftwareOptionService>();
                    _originalSoftwareOptionDto = await softwareOptionService.GetSoftwareOptionByIdAsync(softwareOptionIdToLoad);
                }

                if (_originalSoftwareOptionDto == null)
                {
                    _notificationService.ShowError($"Software Option with ID {softwareOptionIdToLoad} not found.", "Load Error");
                    PrimaryName = "ERROR: Not Found"; OnPropertyChanged(nameof(ViewTitle)); IsLoading = false; return;
                }

                SoftwareOptionId = _originalSoftwareOptionDto.SoftwareOptionId;
                PrimaryName = _originalSoftwareOptionDto.PrimaryName;
                AlternativeNames = _originalSoftwareOptionDto.AlternativeNames;
                SourceFileName = _originalSoftwareOptionDto.SourceFileName;
                PrimaryOptionNumberDisplay = _originalSoftwareOptionDto.PrimaryOptionNumberDisplay;
                Notes = _originalSoftwareOptionDto.Notes;
                ControlSystemId = _originalSoftwareOptionDto.ControlSystemId;
                Version = _originalSoftwareOptionDto.Version;
                LastModifiedDate = _originalSoftwareOptionDto.LastModifiedDate;
                LastModifiedBy = _originalSoftwareOptionDto.LastModifiedBy;

                OptionNumbers.Clear();
                _originalSoftwareOptionDto.OptionNumbers?.ForEach(dto => OptionNumbers.Add(new OptionNumberViewModel { OptionNumber = dto.OptionNumber, ItemChangedCallback = MarkOptionNumbersAsModified }));

                Requirements.Clear();
                _originalSoftwareOptionDto.Requirements?.ForEach(dto => Requirements.Add(new RequirementViewModel { RequirementType = dto.RequirementType, Condition = dto.Condition, GeneralRequiredValue = dto.GeneralRequiredValue ?? string.Empty, RequiredSoftwareOptionId = dto.RequiredSoftwareOptionId, RequiredSpecCodeDefinitionId = dto.RequiredSpecCodeDefinitionId, OspFileName = dto.OspFileName, OspFileVersion = dto.OspFileVersion, Notes = dto.Notes, ItemChangedCallback = MarkRequirementsAsModified }));

                SpecificationCodes.Clear();
                _originalSoftwareOptionDto.SpecificationCodes?.ForEach(dto =>
                {
                    var vm = new SpecCodeViewModel
                    {
                        Category = dto.Category,
                        SpecCodeNo = dto.SpecCodeNo,
                        SpecCodeBit = dto.SpecCodeBit,
                        Description = dto.Description,
                        IsDescriptionReadOnly = true,
                        SoftwareOptionActivationRuleId = dto.SoftwareOptionActivationRuleId,
                        SpecificInterpretation = dto.SpecificInterpretation,
                        ItemChangedCallback = MarkSpecCodesAsModified
                    };
                    vm.SpecCodeDisplayName = $"{vm.Category} - {vm.SpecCodeNo}/{vm.SpecCodeBit}";
                    SpecificationCodes.Add(vm);
                });

                ActivationRules.Clear();
                _originalSoftwareOptionDto.ActivationRules?.ForEach(dto => ActivationRules.Add(new ActivationRuleViewModel { RuleName = dto.RuleName, ActivationSetting = dto.ActivationSetting, Notes = dto.Notes, ItemChangedCallback = MarkActivationRulesAsModified }));

                ResetDirtyFlags();
                OnPropertyChanged(nameof(ViewTitle));
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error loading option: {ex.Message}", "Load Error");
                PrimaryName = "ERROR: Load Failed"; OnPropertyChanged(nameof(ViewTitle));
            }
            finally { IsLoading = false; }
        }

        public async Task<bool> ExecuteSaveAsync()
        {
            if (string.IsNullOrWhiteSpace(PrimaryName)) { _notificationService.ShowWarning("Primary Name is required.", "Validation Error"); return false; }
            if (!ControlSystemId.HasValue || ControlSystemId.Value <= 0) { _notificationService.ShowWarning("Control System is required.", "Validation Error"); return false; }

            IsLoading = true;
            string currentUser = _authStateProvider.CurrentUser?.UserName ?? "System";
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var softwareOptionService = scope.ServiceProvider.GetRequiredService<ISoftwareOptionService>();
                    var specCodeCreateDtos = this.SpecificationCodes.Select(vm => new SoftwareOptionSpecificationCodeCreateDto { Category = vm.Category, SpecCodeNo = vm.SpecCodeNo, SpecCodeBit = vm.SpecCodeBit, Description = vm.Description, SoftwareOptionActivationRuleId = vm.SoftwareOptionActivationRuleId, SpecificInterpretation = vm.SpecificInterpretation }).ToList();

                    if (_isNewSoftwareOption)
                    {
                        var createDto = new CreateSoftwareOptionCommandDto { PrimaryName = this.PrimaryName, AlternativeNames = this.AlternativeNames, SourceFileName = this.SourceFileName, PrimaryOptionNumberDisplay = this.PrimaryOptionNumberDisplay, Notes = this.Notes, ControlSystemId = this.ControlSystemId, OptionNumbers = this.OptionNumbers.Select(vm => new OptionNumberRegistryCreateDto { OptionNumber = vm.OptionNumber }).ToList(), Requirements = this.Requirements.Select(vm => new RequirementCreateDto { RequirementType = vm.RequirementType, Condition = vm.Condition, GeneralRequiredValue = vm.GeneralRequiredValue ?? string.Empty, RequiredSoftwareOptionId = vm.RequiredSoftwareOptionId, RequiredSpecCodeDefinitionId = vm.RequiredSpecCodeDefinitionId, OspFileName = vm.OspFileName, OspFileVersion = vm.OspFileVersion, Notes = vm.Notes }).ToList(), SpecificationCodes = specCodeCreateDtos, ActivationRules = this.ActivationRules.Select(vm => new SoftwareOptionActivationRuleCreateDto { RuleName = vm.RuleName, ActivationSetting = vm.ActivationSetting, Notes = vm.Notes }).ToList() };
                        var createdOption = await softwareOptionService.CreateSoftwareOptionAsync(createDto, currentUser);
                        if (createdOption != null)
                        {
                            await LoadSoftwareOptionAsync(createdOption.SoftwareOptionId);
                            _isNewSoftwareOption = false; ResetDirtyFlags();
                            _notificationService.ShowSuccess($"Software Option '{createdOption.PrimaryName}' created successfully.", "Save Successful");
                            return true;
                        }
                        else { _notificationService.ShowError("Failed to create Software Option.", "Save Failed"); return false; }
                    }
                    else
                    {
                        if (_originalSoftwareOptionDto == null) { _notificationService.ShowError("Original option data is missing for update.", "Save Error"); return false; }
                        if (!_isScalarModified && !_isOptionNumbersModified && !_isRequirementsModified && !_isSpecCodesModified && !_isActivationRulesModified) { _notificationService.ShowInformation("No changes detected to save.", "Save Information"); return true; }

                        var updateDto = new UpdateSoftwareOptionCommandDto { SoftwareOptionId = this.SoftwareOptionId, PrimaryName = this.PrimaryName, AlternativeNames = this.AlternativeNames, SourceFileName = this.SourceFileName, PrimaryOptionNumberDisplay = this.PrimaryOptionNumberDisplay, Notes = this.Notes, CheckedBy = this.CheckedBy, CheckedDate = this.CheckedDate, ControlSystemId = this.ControlSystemId, OptionNumbers = _isOptionNumbersModified ? this.OptionNumbers.Select(vm => new OptionNumberRegistryCreateDto { OptionNumber = vm.OptionNumber }).ToList() : null, Requirements = _isRequirementsModified ? this.Requirements.Select(vm => new RequirementCreateDto { RequirementType = vm.RequirementType, Condition = vm.Condition, GeneralRequiredValue = vm.GeneralRequiredValue ?? string.Empty, RequiredSoftwareOptionId = vm.RequiredSoftwareOptionId, RequiredSpecCodeDefinitionId = vm.RequiredSpecCodeDefinitionId, OspFileName = vm.OspFileName, OspFileVersion = vm.OspFileVersion, Notes = vm.Notes }).ToList() : null, SpecificationCodes = _isSpecCodesModified ? specCodeCreateDtos : null, ActivationRules = _isActivationRulesModified ? this.ActivationRules.Select(vm => new SoftwareOptionActivationRuleCreateDto { RuleName = vm.RuleName, ActivationSetting = vm.ActivationSetting, Notes = vm.Notes }).ToList() : null };
                        var updatedOption = await softwareOptionService.UpdateSoftwareOptionAsync(updateDto, currentUser);
                        if (updatedOption != null)
                        {
                            await LoadSoftwareOptionAsync(this.SoftwareOptionId);
                            _notificationService.ShowSuccess($"Software Option '{updatedOption.PrimaryName}' updated successfully.", "Save Successful");
                            return true;
                        }
                        else { _notificationService.ShowError("Failed to update Software Option.", "Save Failed"); return false; }
                    }
                }
            }
            catch (DbUpdateException dbEx)
            {
                if (dbEx.InnerException is Microsoft.Data.Sqlite.SqliteException sqliteEx && sqliteEx.SqliteErrorCode == 19) { _notificationService.ShowError($"Failed to save: A record with the same unique key (e.g., Spec Code No, Bit, Control System, Category) already exists. Details: {sqliteEx.Message}", "Save Error - Unique Constraint"); } else { _notificationService.ShowError($"Error saving to database: {dbEx.GetBaseException().Message}", "Database Save Error"); }
                return false;
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"An unexpected error occurred while saving: {ex.Message}", "Save Error");
                return false;
            }
            finally { IsLoading = false; }
        }

        #region Private Helper Methods
        private async Task LoadInitialLookupsAsync()
        {
            IsLoading = true;
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var softwareOptionService = scope.ServiceProvider.GetRequiredService<ISoftwareOptionService>();

                    var controlSystems = await softwareOptionService.GetControlSystemLookupsAsync();
                    Application.Current.Dispatcher.Invoke(() => { AvailableControlSystems.Clear(); if (controlSystems != null) foreach (var cs in controlSystems) AvailableControlSystems.Add(cs); OnPropertyChanged(nameof(AvailableControlSystems)); });

                    var allSoftwareOptions = await softwareOptionService.GetAllSoftwareOptionsAsync();
                    Application.Current.Dispatcher.Invoke(() => { AvailableSoftwareOptionsForRequirements.Clear(); if (allSoftwareOptions != null) { foreach (var so in allSoftwareOptions.OrderBy(s => s.PrimaryName)) { AvailableSoftwareOptionsForRequirements.Add(new SoftwareOptionLookupDto { SoftwareOptionId = so.SoftwareOptionId, PrimaryName = so.PrimaryName, ControlSystemId = so.ControlSystemId }); } } OnPropertyChanged(nameof(AvailableSoftwareOptionsForRequirements)); });

                    await LoadSpecCodeDefinitionsForRequirementsAsync();

                    if (!AvailableActivationRules.Any())
                    {
                        AvailableActivationRules.Add(new ActivationRuleLookupDto { Id = 1, DisplayName = "AR001 - Test Rule 1" });
                        AvailableActivationRules.Add(new ActivationRuleLookupDto { Id = 2, DisplayName = "AR002 - Test Rule 2" });
                        OnPropertyChanged(nameof(AvailableActivationRules));
                    }
                }
            }
            catch (Exception ex) { _notificationService.ShowError($"Error loading lookups: {ex.Message}", "Load Error"); }
            finally { IsLoading = false; }
        }

        private async Task LoadSpecCodeDefinitionsForRequirementsAsync()
        {
            if (!ControlSystemId.HasValue || ControlSystemId.Value <= 0)
            {
                Application.Current.Dispatcher.Invoke(() => { AvailableSpecCodesForRequirements.Clear(); OnPropertyChanged(nameof(AvailableSpecCodesForRequirements)); });
                return;
            }

            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var softwareOptionService = scope.ServiceProvider.GetRequiredService<ISoftwareOptionService>();
                    var specCodes = await softwareOptionService.GetSpecCodeDefinitionsForControlSystemAsync(ControlSystemId.Value);
                    Application.Current.Dispatcher.Invoke(() => { AvailableSpecCodesForRequirements.Clear(); if (specCodes != null) { foreach (var scd in specCodes.OrderBy(s => s.Category).ThenBy(s => s.SpecCodeNo).ThenBy(s => s.SpecCodeBit)) { AvailableSpecCodesForRequirements.Add(new SpecCodeDefinitionLookupDto { SpecCodeDefinitionId = scd.SpecCodeDefinitionId, DisplayName = $"{scd.Category} - {scd.SpecCodeNo}/{scd.SpecCodeBit}: {scd.Description?.Substring(0, Math.Min(scd.Description.Length, 30)) ?? ""}{(scd.Description?.Length > 30 ? "..." : "")}", ControlSystemId = scd.ControlSystemId }); } } OnPropertyChanged(nameof(AvailableSpecCodesForRequirements)); });
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error loading spec code definitions for requirements: {ex.Message}", "Load Error");
                Application.Current.Dispatcher.Invoke(() => { AvailableSpecCodesForRequirements.Clear(); OnPropertyChanged(nameof(AvailableSpecCodesForRequirements)); });
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
            newOptionNumber.ItemChangedCallback = MarkOptionNumbersAsModified;
            OptionNumbers.Add(newOptionNumber);
        }

        private void ExecuteRemoveOptionNumber()
        {
            if (SelectedOptionNumber != null) OptionNumbers.Remove(SelectedOptionNumber);
        }

        private void ExecuteAddRequirement()
        {
            var newReqVm = new RequirementViewModel { RequirementType = RequirementViewModel.AvailableRequirementTypes.FirstOrDefault() ?? string.Empty };
            newReqVm.ItemChangedCallback = MarkRequirementsAsModified;
            Requirements.Add(newReqVm);
        }

        private void ExecuteRemoveRequirement()
        {
            if (SelectedRequirement != null) Requirements.Remove(SelectedRequirement);
        }

        private void ExecuteRemoveSpecificationCode()
        {
            if (SelectedSpecificationCode != null) SpecificationCodes.Remove(SelectedSpecificationCode);
        }

        private void ExecuteShowSpecCodeDialogForAdd()
        {
            if (!ControlSystemId.HasValue || ControlSystemId.Value <= 0) { _notificationService.ShowWarning("Please select a Control System for the Software Option first.", "Control System Required"); return; }
            string currentControlSystemName = AvailableControlSystems.FirstOrDefault(cs => cs.ControlSystemId == ControlSystemId.Value)?.Name ?? "Unknown";
            var newSpecCodeInstance = new SpecCodeViewModel { IsDescriptionReadOnly = false };
            var dialogViewModel = new EditSpecCodeDialogViewModel(_scopeFactory, newSpecCodeInstance, currentControlSystemName, ControlSystemId.Value, AvailableActivationRules);
            ShowSpecCodeDialog("Add Specification Code", dialogViewModel, true);
        }

        private void ExecuteShowSpecCodeDialogForEdit(SpecCodeViewModel? specCodeToEdit)
        {
            if (specCodeToEdit == null) return;
            if (!ControlSystemId.HasValue || ControlSystemId.Value <= 0) { _notificationService.ShowWarning("The parent Software Option's Control System is not set.", "Control System Required"); return; }
            string currentControlSystemName = AvailableControlSystems.FirstOrDefault(cs => cs.ControlSystemId == ControlSystemId.Value)?.Name ?? "Unknown";
            var dialogViewModel = new EditSpecCodeDialogViewModel(_scopeFactory, specCodeToEdit, currentControlSystemName, ControlSystemId.Value, AvailableActivationRules);
            ShowSpecCodeDialog("Edit Specification Code", dialogViewModel, false);
        }

        private void ShowSpecCodeDialog(string title, EditSpecCodeDialogViewModel dialogViewModel, bool isAddingNew)
        {
            using (var dialogScope = _scopeFactory.CreateScope())
            {
                var dialogView = dialogScope.ServiceProvider.GetRequiredService<EditSpecCodeDialog>();
                dialogView.DataContext = dialogViewModel;
                var dialogWindow = new Window { Title = title, Content = dialogView, Width = 480, Height = 600, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = Application.Current?.MainWindow, ShowInTaskbar = false, ResizeMode = ResizeMode.NoResize };
                SpecCodeViewModel? finalItem = null;
                Action<SpecCodeViewModel?>? dialogClosedHandler = null;
                dialogClosedHandler = (itemFromDialog) => { finalItem = itemFromDialog; if (dialogViewModel != null && dialogClosedHandler != null) dialogViewModel.DialogClosed -= dialogClosedHandler; };
                dialogViewModel.DialogClosed += dialogClosedHandler;
                dialogViewModel.CloseDialogWindowAction = (result) => { dialogWindow.DialogResult = result; dialogWindow.Close(); };
                bool? resultState = dialogWindow.ShowDialog();
                if (resultState == true && finalItem != null)
                {
                    finalItem.ItemChangedCallback = MarkSpecCodesAsModified;
                    if (isAddingNew)
                    {
                        SpecificationCodes.Add(finalItem);
                        SelectedSpecificationCode = finalItem;
                    }
                    MarkSpecCodesAsModified();
                }
            }
        }
        #endregion
    }
}
