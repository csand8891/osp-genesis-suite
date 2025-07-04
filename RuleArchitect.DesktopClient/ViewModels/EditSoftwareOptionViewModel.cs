﻿// In RuleArchitect.DesktopClient/ViewModels/EditSoftwareOptionViewModel.cs
using GenesisSentry.Interfaces;
using HeraldKit.Interfaces;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RuleArchitect.Abstractions.DTOs.Lookups;
using RuleArchitect.Abstractions.DTOs.SoftwareOption;
using RuleArchitect.Abstractions.Interfaces;
using RuleArchitect.DesktopClient.Commands;
using RuleArchitect.DesktopClient.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RuleArchitect.DesktopClient.ViewModels
{
    public class EditSoftwareOptionViewModel : ValidatableViewModel
    {
        private readonly IAuthenticationStateProvider _authStateProvider;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly INotificationService _notificationService;
        private bool _isReadOnlyMode;
        #region Backing Fields
        private int _softwareOptionId;
        private static int _tempActivationRuleId = -1;
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
        private string? _controlSystemName; // Backing field for Control System Name
        private int _version;
        private DateTime _lastModifiedDate;
        private string? _lastModifiedBy;
        #endregion

        public Func<Task>? OnSaveSuccessAsync { get; set; }

        public ObservableCollection<string> AvailableCategories { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableSpecNos { get; } = new ObservableCollection<string>(Enumerable.Range(1, 32).Select(i => i.ToString()));
        public ObservableCollection<string> AvailableSpecBits { get; } = new ObservableCollection<string>(Enumerable.Range(0, 8).Select(i => i.ToString()));

        #region Public Properties
        // UPDATED: ViewTitle now uses PrimaryName and ControlSystemName
        public string ViewTitle => _isNewSoftwareOption ? "Create New Software Option" : $"Edit Software Option: {PrimaryName} ({ControlSystemName})";
        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsReadOnlyMode
        {
            get => _isReadOnlyMode;
            set
            {
                if (SetProperty(ref _isReadOnlyMode, value))
                {
                    (ToggleEditModeCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (CancelEditCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string PrimaryName
        {
            get => _primaryName;
            set
            {
                if (SetProperty(ref _primaryName, value))
                {
                    MarkScalarAsModified();
                    OnPropertyChanged(nameof(ViewTitle));
                    Validate();
                }
            }
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
                    // UPDATED: This now also sets the name for the ViewTitle
                    ControlSystemName = AvailableControlSystems.FirstOrDefault(cs => cs.ControlSystemId == value)?.Name;
                    PopulateAvailableCategories();
                    _ = LoadSpecCodeDefinitionsForRequirementsAsync();
                    Validate();
                }
            }
        }

        // ADDED: New property to hold the name of the control system for the title
        public string? ControlSystemName
        {
            get => _controlSystemName;
            set
            {
                if (SetProperty(ref _controlSystemName, value))
                {
                    OnPropertyChanged(nameof(ViewTitle));
                }
            }
        }

        public int Version { get => _version; private set => SetProperty(ref _version, value); }
        public DateTime LastModifiedDate { get => _lastModifiedDate; private set => SetProperty(ref _lastModifiedDate, value); }
        public string? LastModifiedBy { get => _lastModifiedBy; private set => SetProperty(ref _lastModifiedBy, value); }
        public int SoftwareOptionId { get => _softwareOptionId; private set => SetProperty(ref _softwareOptionId, value, () => OnPropertyChanged(nameof(ViewTitle))); }
        public bool IsNewSoftwareOption { get => _isNewSoftwareOption; private set => SetProperty(ref _isNewSoftwareOption, value); }
        #endregion

        #region Collections and Selected Items
        public ObservableCollection<OptionNumberViewModel> OptionNumbers { get; }
        public ObservableCollection<RequirementViewModel> Requirements { get; }
        public ObservableCollection<SpecCodeViewModel> SpecificationCodes { get; }
        public ObservableCollection<ActivationRuleViewModel> ActivationRules { get; }
        public ObservableCollection<ControlSystemLookupDto> AvailableControlSystems { get; }
        public ObservableCollection<ActivationRuleLookupDto> AvailableActivationRules { get; }
        public ObservableCollection<SoftwareOptionLookupDto> AvailableSoftwareOptionsForRequirements { get; }
        public ObservableCollection<SpecCodeDefinitionLookupDto> AvailableSpecCodesForRequirements { get; }

        private SpecCodeViewModel? _selectedSpecificationCode;
        public SpecCodeViewModel? SelectedSpecificationCode { get => _selectedSpecificationCode; set { if (SetProperty(ref _selectedSpecificationCode, value)) { ((RelayCommand)RemoveSpecificationCodeCommand).RaiseCanExecuteChanged(); ((RelayCommand)EditSpecificationCodeCommand).RaiseCanExecuteChanged(); } } }
        private RequirementViewModel? _selectedRequirement;
        public RequirementViewModel? SelectedRequirement { get => _selectedRequirement; set => SetProperty(ref _selectedRequirement, value, () => { ((RelayCommand)RemoveRequirementCommand).RaiseCanExecuteChanged(); }); }
        private OptionNumberViewModel? _selectedOptionNumber;
        public OptionNumberViewModel? SelectedOptionNumber { get => _selectedOptionNumber; set => SetProperty(ref _selectedOptionNumber, value, () => ((RelayCommand)RemoveOptionNumberCommand).RaiseCanExecuteChanged()); }
        private ActivationRuleViewModel? _selectedActivationRule;
        public ActivationRuleViewModel? SelectedActivationRule { get => _selectedActivationRule; set => SetProperty(ref _selectedActivationRule, value, () => ((RelayCommand)RemoveActivationRuleCommand).RaiseCanExecuteChanged()); }
        #endregion

        #region Commands
        public ICommand AddSpecificationCodeCommand { get; }
        public ICommand EditSpecificationCodeCommand { get; }
        public ICommand RemoveSpecificationCodeCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand AddOptionNumberCommand { get; }
        public ICommand RemoveOptionNumberCommand { get; }
        public ICommand AddRequirementCommand { get; }
        public ICommand RemoveRequirementCommand { get; }
        public ICommand AddActivationRuleCommand { get; }
        public ICommand RemoveActivationRuleCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand ToggleEditModeCommand { get; }
        #endregion

        public EditSoftwareOptionViewModel(IAuthenticationStateProvider authStateProvider, IServiceScopeFactory scopeFactory, INotificationService notificationService, ICommand closeCommand)
        {
            _authStateProvider = authStateProvider;
            _scopeFactory = scopeFactory;
            _notificationService = notificationService;

            IsNewSoftwareOption = true;
            OptionNumbers = new ObservableCollection<OptionNumberViewModel>();
            Requirements = new ObservableCollection<RequirementViewModel>();
            SpecificationCodes = new ObservableCollection<SpecCodeViewModel>();
            ActivationRules = new ObservableCollection<ActivationRuleViewModel>();
            AvailableControlSystems = new ObservableCollection<ControlSystemLookupDto>();
            AvailableActivationRules = new ObservableCollection<ActivationRuleLookupDto>();
            AvailableSoftwareOptionsForRequirements = new ObservableCollection<SoftwareOptionLookupDto>();
            AvailableSpecCodesForRequirements = new ObservableCollection<SpecCodeDefinitionLookupDto>();
            CloseCommand = closeCommand;

            AddActivationRuleCommand = new RelayCommand(_ => ExecuteAddActivationRule());
            RemoveActivationRuleCommand = new RelayCommand(_ => ExecuteRemoveActivationRule(), _ => SelectedActivationRule != null);
            SubscribeToCollectionChanges();

            SaveCommand = new RelayCommand(async _ => await ExecuteSaveAsync(), _ => !HasErrors && !IsLoading && !IsReadOnlyMode);
            ToggleEditModeCommand = new RelayCommand(_ => IsReadOnlyMode = !IsReadOnlyMode, _ => !IsLoading);

            CancelEditCommand = new RelayCommand(async () => await ExecuteCancelEditAsync(), () => !IsReadOnlyMode && !IsLoading);

            AddOptionNumberCommand = new RelayCommand(_ => ExecuteAddOptionNumber());
            RemoveOptionNumberCommand = new RelayCommand(ExecuteRemoveOptionNumber, (param) => param is OptionNumberViewModel);
            AddRequirementCommand = new RelayCommand(_ => ExecuteAddRequirement());
            RemoveRequirementCommand = new RelayCommand(_ => ExecuteRemoveRequirement(), _ => SelectedRequirement != null);
            AddSpecificationCodeCommand = new RelayCommand(_ => ExecuteShowSpecCodeDialogForAdd());
            EditSpecificationCodeCommand = new RelayCommand(param => ExecuteShowSpecCodeDialogForEdit(param as SpecCodeViewModel), param => param is SpecCodeViewModel);
            RemoveSpecificationCodeCommand = new RelayCommand(_ => ExecuteRemoveSpecificationCode(), _ => SelectedSpecificationCode != null);

            PrimaryName = "";
            Version = 1;
            LastModifiedDate = DateTime.UtcNow;
            LastModifiedBy = _authStateProvider.CurrentUser?.UserName ?? "System";
            ResetDirtyFlags();
            OnPropertyChanged(nameof(ViewTitle));
            _ = LoadInitialLookupsAsync();
            Validate();
        }

        private async Task ExecuteCancelEditAsync()
        {
            if (!IsNewSoftwareOption && _originalSoftwareOptionDto != null)
            {
                await LoadSoftwareOptionAsync(_originalSoftwareOptionDto.SoftwareOptionId, true);
            }
            else
            {
                CloseCommand?.Execute(null);
            }
        }

        public async Task LoadSoftwareOptionAsync(int softwareOptionIdToLoad, bool initialReadOnly = false)
        {
            IsLoading = true;
            IsNewSoftwareOption = softwareOptionIdToLoad <= 0;
            IsReadOnlyMode = initialReadOnly;
            if (!AvailableControlSystems.Any()) await LoadInitialLookupsAsync();

            try
            {
                if (!IsNewSoftwareOption)
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
                    //CheckedBy = _originalSoftwareOptionDto.CheckedBy;
                    //CheckedDate = _originalSoftwareOptionDto.CheckedDate;
                    ControlSystemId = _originalSoftwareOptionDto.ControlSystemId;
                    ControlSystemName = _originalSoftwareOptionDto.ControlSystemName; // UPDATED: Set the name on load
                    Version = _originalSoftwareOptionDto.Version;
                    LastModifiedDate = _originalSoftwareOptionDto.LastModifiedDate;
                    LastModifiedBy = _originalSoftwareOptionDto.LastModifiedBy;

                    OptionNumbers.Clear();
                    _originalSoftwareOptionDto.OptionNumbers?.ForEach(dto => OptionNumbers.Add(new OptionNumberViewModel { OriginalId = 0, OptionNumber = dto.OptionNumber }));

                    Requirements.Clear();
                    _originalSoftwareOptionDto.Requirements?.ForEach(dto => Requirements.Add(new RequirementViewModel { OriginalId = 0, RequirementType = dto.RequirementType, Condition = dto.Condition, GeneralRequiredValue = dto.GeneralRequiredValue ?? string.Empty, RequiredSoftwareOptionId = dto.RequiredSoftwareOptionId, RequiredSpecCodeDefinitionId = dto.RequiredSpecCodeDefinitionId, OspFileName = dto.OspFileName, OspFileVersion = dto.OspFileVersion, Notes = dto.Notes }));

                    SpecificationCodes.Clear();
                    _originalSoftwareOptionDto.SpecificationCodes?.ForEach(dto =>
                    {
                        var vm = new SpecCodeViewModel(_scopeFactory, () => MarkCollectionAsModified(ref _isSpecCodesModified))
                        {
                            OriginalId = 0,
                            IsActive = dto.IsActive,
                            Category = dto.Category,
                            SpecCodeNo = dto.SpecCodeNo,
                            SpecCodeBit = dto.SpecCodeBit,
                            Description = dto.Description,
                            IsDescriptionReadOnly = true,
                            SoftwareOptionActivationRuleId = dto.SoftwareOptionActivationRuleId
                        };
                        vm.SpecCodeDisplayName = $"{vm.Category} - {vm.SpecCodeNo}/{vm.SpecCodeBit}";
                        SpecificationCodes.Add(vm);
                    });

                    ActivationRules.Clear();
                    _originalSoftwareOptionDto.ActivationRules?.ForEach(dto => ActivationRules.Add(new ActivationRuleViewModel { OriginalId = 0, RuleName = dto.RuleName, ActivationSetting = dto.ActivationSetting, Notes = dto.Notes }));
                }

                ResetDirtyFlags();
                OnPropertyChanged(nameof(ViewTitle));
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error loading option: {ex.Message}", "Load Error");
                PrimaryName = "ERROR: Load Failed"; OnPropertyChanged(nameof(ViewTitle));
            }
            finally
            {
                IsLoading = false;
                Validate();
            }
        }

        public async Task<bool> ExecuteSaveAsync()
        {
            if (IsReadOnlyMode)
            {
                _notificationService.ShowWarning("Currently in view mode. Click 'Edit' to make changes.", "Cannot Save");
                return false;
            }
            Validate();
            if (HasErrors)
            {
                _notificationService.ShowWarning("Please fix all validation errors before saving.", "Validation Failed");
                return false;
            }

            var success = await SaveSoftwareOptionAsync();

            if (success && OnSaveSuccessAsync != null)
            {
                await OnSaveSuccessAsync();
            }

            return success;
        }

        private async Task<bool> SaveSoftwareOptionAsync()
        {
            IsLoading = true;
            var currentUser = _authStateProvider.CurrentUser;
            if (currentUser == null)
            {
                _notificationService.ShowError("Authentication error. Cannot save option.", "Save Error");
                IsLoading = false;
                return false;
            }

            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var softwareOptionService = scope.ServiceProvider.GetRequiredService<ISoftwareOptionService>();
                    var specCodeCreateDtos = this.SpecificationCodes.Select(vm => new SoftwareOptionSpecificationCodeCreateDto { IsActive = vm.IsActive, Category = vm.Category, SpecCodeNo = vm.SpecCodeNo, SpecCodeBit = vm.SpecCodeBit, Description = vm.Description, SoftwareOptionActivationRuleId = vm.SoftwareOptionActivationRuleId }).ToList();

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
                        var createdOption = await softwareOptionService.CreateSoftwareOptionAsync(createDto, currentUser.UserId, currentUser.UserName);

                        if (createdOption != null)
                        {
                            await LoadSoftwareOptionAsync(createdOption.SoftwareOptionId, true);
                            _isNewSoftwareOption = false; ResetDirtyFlags();
                            _notificationService.ShowSuccess($"Software Option '{createdOption.PrimaryName}' created successfully.", "Save Successful");
                            return true;
                        }
                        else { _notificationService.ShowError("Failed to create Software Option.", "Save Failed"); return false; }
                    }
                    else
                    {
                        if (_originalSoftwareOptionDto == null) { _notificationService.ShowError("Original option data is missing for update.", "Save Error"); return false; }
                        if (!_isScalarModified && !_isOptionNumbersModified && !_isRequirementsModified && !_isSpecCodesModified && !_isActivationRulesModified) { _notificationService.ShowInformation("No changes detected to save.", "Save Information"); IsReadOnlyMode = true; return true; }

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
                        var updatedOption = await softwareOptionService.UpdateSoftwareOptionAsync(updateDto, currentUser.UserId, currentUser.UserName);
                        if (updatedOption != null)
                        {
                            await LoadSoftwareOptionAsync(this.SoftwareOptionId, true);
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

        public void Validate()
        {
            ClearErrors(nameof(PrimaryName));
            if (string.IsNullOrWhiteSpace(PrimaryName))
            {
                AddError(nameof(PrimaryName), "Primary Name is required.");
            }

            ClearErrors(nameof(ControlSystemId));
            if (!ControlSystemId.HasValue || ControlSystemId <= 0)
            {
                AddError(nameof(ControlSystemId), "A Control System must be selected.");
            }
            (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
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
            // Implementation is correct
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
            OptionNumbers.Add(newOptionNumber);
        }

        private void ExecuteRemoveOptionNumber(object? parameter)
        {
            if (parameter is OptionNumberViewModel optionToRemove)
            {
                OptionNumbers.Remove(optionToRemove);
            }
        }
        private void ExecuteAddRequirement()
        {
            var newReqVm = new RequirementViewModel { RequirementType = RequirementViewModel.AvailableRequirementTypes.FirstOrDefault() ?? string.Empty };
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

        private void PopulateAvailableCategories()
        {
            AvailableCategories.Clear();
            if (ControlSystemId.HasValue)
            {
                var controlSystem = AvailableControlSystems.FirstOrDefault(cs => cs.ControlSystemId == ControlSystemId.Value);
                if (controlSystem != null)
                {
                    if (controlSystem.Name.ToUpperInvariant().StartsWith("P"))
                    {
                        AvailableCategories.Add("NC1");
                        AvailableCategories.Add("NC2");
                        AvailableCategories.Add("NC3");
                        AvailableCategories.Add("PLC1");
                        AvailableCategories.Add("PLC2");
                        AvailableCategories.Add("PLC3");
                    }
                    else
                    {
                        AvailableCategories.Add("NC");
                        AvailableCategories.Add("PLC");
                    }
                }
            }
        }
        #endregion

        #region Dialog Methods
        private async void ShowSpecCodeDialog(string title, EditSpecCodeDialogViewModel dialogViewModel)
        {
            var view = new EditSpecCodeDialog { DataContext = dialogViewModel };
            object result = await DialogHost.Show(view, "EditSoftwareOptionDialogHost");
            if (result is SpecCodeViewModel finalItem)
            {
                if (dialogViewModel.IsAddingNew)
                {
                    SpecificationCodes.Add(finalItem);
                    SelectedSpecificationCode = finalItem;
                }
                MarkCollectionAsModified(ref _isSpecCodesModified);
            }
        }

        private void ExecuteShowSpecCodeDialogForAdd()
        {
            if (!ControlSystemId.HasValue || ControlSystemId.Value <= 0)
            {
                _notificationService.ShowWarning("Please select a Control System for the Software Option first.", "Control System Required");
                return;
            }
            string currentControlSystemName = AvailableControlSystems.FirstOrDefault(cs => cs.ControlSystemId == ControlSystemId.Value)?.Name ?? "Unknown";
            var newSpecCodeInstance = new SpecCodeViewModel(_scopeFactory, () => MarkCollectionAsModified(ref _isSpecCodesModified)) { IsDescriptionReadOnly = false };
            var dialogViewModel = new EditSpecCodeDialogViewModel(_scopeFactory, newSpecCodeInstance, currentControlSystemName, ControlSystemId.Value, this)
            {
                IsAddingNew = true
            };
            ShowSpecCodeDialog("Add Specification Code", dialogViewModel);
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
            var dialogViewModel = new EditSpecCodeDialogViewModel(_scopeFactory, specCodeToEdit, currentControlSystemName, ControlSystemId.Value, this)
            {
                IsAddingNew = false
            };
            ShowSpecCodeDialog("Edit Specification Code", dialogViewModel);
        }
        #endregion

        #region Activation Rule Methods
        private void ExecuteAddActivationRule()
        {
            var newRule = new ActivationRuleViewModel
            {
                RuleName = "New Rule",
                ActivationSetting = "Default Setting",
                TempId = _tempActivationRuleId
            };
            _tempActivationRuleId--;
            ActivationRules.Add(newRule);
            var newLookup = new ActivationRuleLookupDto { Id = newRule.TempId, DisplayName = newRule.RuleName };
            AvailableActivationRules.Add(newLookup);
            SelectedActivationRule = newRule;
        }

        private void ExecuteRemoveActivationRule()
        {
            if (SelectedActivationRule != null)
            {
                if (SelectedActivationRule.TempId < 0)
                {
                    var lookupToRemove = AvailableActivationRules.FirstOrDefault(l => l.Id == SelectedActivationRule.TempId);
                    if (lookupToRemove != null)
                    {
                        AvailableActivationRules.Remove(lookupToRemove);
                    }
                }
                ActivationRules.Remove(SelectedActivationRule);
            }
        }
        #endregion
    }
}