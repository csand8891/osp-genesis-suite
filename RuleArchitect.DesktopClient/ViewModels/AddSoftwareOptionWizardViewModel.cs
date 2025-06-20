using HeraldKit.Interfaces;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using RuleArchitect.Abstractions.DTOs.Lookups;
using RuleArchitect.Abstractions.DTOs.SoftwareOption;
using RuleArchitect.Abstractions.Interfaces;
using RuleArchitect.DesktopClient.Commands;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

namespace RuleArchitect.DesktopClient.ViewModels
{
    /// <summary>
    /// ViewModel for the Add Software Option Wizard.
    /// Manages wizard steps, user input, validation, and saving logic for creating a new software option.
    /// </summary>
    public class AddSoftwareOptionWizardViewModel : BaseViewModel
    {
        // Dependency-injected services for data access, notifications, and authentication
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly INotificationService _notificationService;
        private readonly IAuthenticationStateProvider _authStateProvider;

        // Tracks the current step in the wizard (0-based index)
        private int _currentStepIndex;
        public int CurrentStepIndex
        {
            get => _currentStepIndex;
            set
            {
                if (SetProperty(ref _currentStepIndex, value))
                {
                    // Notify UI of step-related property changes
                    OnPropertyChanged(nameof(StepTitle));
                    OnPropertyChanged(nameof(IsOnFirstStep));
                    OnPropertyChanged(nameof(IsOnLastStep));
                    // Update command states when step changes
                    ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)PreviousCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets the title for the current wizard step.
        /// </summary>
        public string StepTitle
        {
            get
            {
                switch (CurrentStepIndex)
                {
                    case 0: return "Step 1: Core Details";
                    case 1: return "Step 2: Option Numbers";
                    case 2: return "Step 3: Specification Codes";
                    case 3: return "Step 4: Requirements";
                    case 4: return "Step 5: Review and Finish";
                    default: return "Create Software Option";
                }
            }
        }

        /// <summary>
        /// Indicates if the wizard is on the first step.
        /// </summary>
        public bool IsOnFirstStep => CurrentStepIndex == 0;
        /// <summary>
        /// Indicates if the wizard is on the last step.
        /// </summary>
        public bool IsOnLastStep => CurrentStepIndex == 4;

        // Main DTO being constructed and eventually saved
        public CreateSoftwareOptionCommandDto NewSoftwareOption { get; }
        // Lookup data for control systems
        public ObservableCollection<ControlSystemLookupDto> AvailableControlSystems { get; }

        // Collection of option number ViewModels for the wizard
        public ObservableCollection<OptionNumberWizardViewModel> OptionNumbers { get; }

        // Collection of specification code ViewModels for the wizard
        public ObservableCollection<SpecCodeViewModel> SpecificationCodes { get; }
        // Lookup data for categories, spec numbers, and bits
        public ObservableCollection<string> AvailableCategories { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableSpecNos { get; } = new ObservableCollection<string>(Enumerable.Range(1, 32).Select(i => i.ToString()));
        public ObservableCollection<string> AvailableSpecBits { get; } = new ObservableCollection<string>(Enumerable.Range(0, 8).Select(i => i.ToString()));

        // Collection of requirement ViewModels for the wizard
        public ObservableCollection<RequirementViewModel> Requirements { get; }
        // Lookup data for requirements step
        public ObservableCollection<SoftwareOptionLookupDto> AvailableSoftwareOptionsForRequirements { get; }
        public ObservableCollection<SpecCodeDefinitionLookupDto> AvailableSpecCodesForRequirements { get; }

        // Wizard navigation and action commands
        public ICommand NextCommand { get; }
        public ICommand PreviousCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        // Commands for managing option numbers
        public ICommand AddOptionNumberCommand { get; }
        public ICommand RemoveOptionNumberCommand { get; }

        // Commands for managing specification codes
        public ICommand AddSpecCodeCommand { get; }
        public ICommand RemoveSpecCodeCommand { get; }

        // Commands for managing requirements
        public ICommand AddRequirementCommand { get; }
        public ICommand RemoveRequirementCommand { get; }

        /// <summary>
        /// Constructor. Initializes collections, commands, and loads lookup data.
        /// </summary>
        public AddSoftwareOptionWizardViewModel(IServiceScopeFactory scopeFactory, INotificationService notificationService, IAuthenticationStateProvider authStateProvider)
        {
            _scopeFactory = scopeFactory;
            _notificationService = notificationService;
            _authStateProvider = authStateProvider;

            NewSoftwareOption = new CreateSoftwareOptionCommandDto();
            AvailableControlSystems = new ObservableCollection<ControlSystemLookupDto>();

            SpecificationCodes = new ObservableCollection<SpecCodeViewModel>();
            OptionNumbers = new ObservableCollection<OptionNumberWizardViewModel>();
            Requirements = new ObservableCollection<RequirementViewModel>();

            AvailableSoftwareOptionsForRequirements = new ObservableCollection<SoftwareOptionLookupDto>();
            AvailableSpecCodesForRequirements = new ObservableCollection<SpecCodeDefinitionLookupDto>();

            // Subscribe to changes in the SpecificationCodes collection to manage event handlers and validation
            SpecificationCodes.CollectionChanged += (s, e) => {
                // Unsubscribe from PropertyChanged on removed items
                if (e.OldItems != null)
                {
                    foreach (SpecCodeViewModel vm in e.OldItems)
                    {
                        vm.PropertyChanged -= SpecCodeViewModel_PropertyChanged;
                    }
                }
                // Subscribe to PropertyChanged on added items
                if (e.NewItems != null)
                {
                    foreach (SpecCodeViewModel vm in e.NewItems)
                    {
                        vm.PropertyChanged += SpecCodeViewModel_PropertyChanged;
                        // Optionally trigger initial validation here
                    }
                }
                // Update navigation command state
                ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
            };

            // Initialize commands with their respective handlers and can-execute logic
            NextCommand = new RelayCommand(GoToNextStep, CanGoToNextStep);
            PreviousCommand = new RelayCommand(GoToPreviousStep, () => !IsOnFirstStep);
            SaveCommand = new RelayCommand(async () => await ExecuteSaveAsync(), () => IsOnLastStep);
            CancelCommand = new RelayCommand(ExecuteCancel);

            AddOptionNumberCommand = new RelayCommand(ExecuteAddOptionNumber);
            RemoveOptionNumberCommand = new RelayCommand(ExecuteRemoveOptionNumber, (param) => param is OptionNumberWizardViewModel);

            AddSpecCodeCommand = new RelayCommand(ExecuteAddSpecCode);
            RemoveSpecCodeCommand = new RelayCommand(ExecuteRemoveSpecCode, (param) => param is SpecCodeViewModel);

            AddRequirementCommand = new RelayCommand(ExecuteAddRequirement);
            RemoveRequirementCommand = new RelayCommand(ExecuteRemoveRequirement, (param) => param is RequirementViewModel);

            // Begin loading lookup data asynchronously
            _ = LoadLookupsAsync();

            // React to changes in the selected control system to update categories and spec code definitions
            this.NewSoftwareOption.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(NewSoftwareOption.ControlSystemId))
                {
                    PopulateAvailableCategories();
                    _ = LoadSpecCodeDefinitionsForRequirementsAsync();
                    // Update all existing spec codes with the new control system context
                    foreach (var specCodeVm in SpecificationCodes)
                    {
                        specCodeVm.ControlSystemId = NewSoftwareOption.ControlSystemId;
                        _ = specCodeVm.CheckDefinitionAsync(_scopeFactory);
                    }
                }
                ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
            };
        }

        /// <summary>
        /// Handles property changes on SpecCodeViewModel instances to trigger validation and update navigation.
        /// </summary>
        private async void SpecCodeViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is SpecCodeViewModel specCodeVm)
            {
                // Only trigger validation for relevant properties
                if (e.PropertyName == nameof(SpecCodeViewModel.Category) ||
                    e.PropertyName == nameof(SpecCodeViewModel.SpecCodeNo) ||
                    e.PropertyName == nameof(SpecCodeViewModel.SpecCodeBit))
                {
                    await specCodeVm.CheckDefinitionAsync(_scopeFactory);
                }
                ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Populates the AvailableCategories collection based on the selected control system.
        /// </summary>
        private void PopulateAvailableCategories()
        {
            AvailableCategories.Clear();
            if (NewSoftwareOption.ControlSystemId.HasValue)
            {
                var controlSystem = AvailableControlSystems.FirstOrDefault(cs => cs.ControlSystemId == NewSoftwareOption.ControlSystemId);
                if (controlSystem != null)
                {
                    if (controlSystem.Name.ToUpperInvariant().StartsWith("P"))
                    {
                        // For "P" systems, use extended categories
                        AvailableCategories.Add("NC1");
                        AvailableCategories.Add("NC2");
                        AvailableCategories.Add("NC3");
                        AvailableCategories.Add("PLC1");
                        AvailableCategories.Add("PLC2");
                        AvailableCategories.Add("PLC3");
                    }
                    else
                    {
                        // For other systems, use standard categories
                        AvailableCategories.Add("NC");
                        AvailableCategories.Add("PLC");
                    }
                }
            }
        }

        // --- Option Number Step Methods ---

        /// <summary>
        /// Adds a new option number entry to the collection.
        /// </summary>
        private void ExecuteAddOptionNumber()
        {
            var newOption = new OptionNumberWizardViewModel { OptionNumber = "" };
            newOption.Validate(); // Show validation error immediately
            OptionNumbers.Add(newOption);
            ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Removes the specified option number entry from the collection.
        /// </summary>
        private void ExecuteRemoveOptionNumber(object parameter)
        {
            if (parameter is OptionNumberWizardViewModel optionToRemove)
            {
                OptionNumbers.Remove(optionToRemove);
                ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
            }
        }

        // --- Specification Code Step Methods ---

        /// <summary>
        /// Maps ViewModel collections to DTOs before saving.
        /// </summary>
        private void PrepareSaveData()
        {
            // Map specification codes
            NewSoftwareOption.SpecificationCodes = SpecificationCodes
                .Select(vm => new SoftwareOptionSpecificationCodeCreateDto
                {
                    Category = vm.Category,
                    SpecCodeNo = vm.SpecCodeNo,
                    SpecCodeBit = vm.SpecCodeBit,
                    Description = vm.Description,
                    SoftwareOptionActivationRuleId = vm.SoftwareOptionActivationRuleId,
                    IsActive = vm.IsActive
                })
                .ToList();

            // Map option numbers
            NewSoftwareOption.OptionNumbers = OptionNumbers
                .Select(vm => new OptionNumberRegistryCreateDto { OptionNumber = vm.OptionNumber })
                .ToList();

            // Map requirements
            NewSoftwareOption.Requirements = Requirements.Select(vm => new RequirementCreateDto
            {
                RequirementType = vm.RequirementType,
                Condition = vm.Condition,
                GeneralRequiredValue = vm.GeneralRequiredValue,
                RequiredSoftwareOptionId = vm.RequiredSoftwareOptionId,
                RequiredSpecCodeDefinitionId = vm.RequiredSpecCodeDefinitionId,
                OspFileName = vm.OspFileName,
                OspFileVersion = vm.OspFileVersion,
                Notes = vm.Notes
            }).ToList();
        }

        /// <summary>
        /// Adds a new specification code entry to the collection.
        /// </summary>
        private void ExecuteAddSpecCode()
        {
            var newSpecCodeVm = new SpecCodeViewModel(_scopeFactory)
            {
                Category = AvailableCategories.FirstOrDefault(),
                SpecCodeNo = "1",
                SpecCodeBit = "0",
                ControlSystemId = NewSoftwareOption.ControlSystemId
            };
            SpecificationCodes.Add(newSpecCodeVm);
            // Trigger initial validation for the new item
            _ = newSpecCodeVm.CheckDefinitionAsync(_scopeFactory);

            ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Removes the specified specification code entry from the collection.
        /// </summary>
        private void ExecuteRemoveSpecCode(object parameter)
        {
            if (parameter is SpecCodeViewModel specCodeToRemove)
            {
                SpecificationCodes.Remove(specCodeToRemove);
                specCodeToRemove.PropertyChanged -= SpecCodeViewModel_PropertyChanged;
                ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
            }
        }

        // --- Requirement Step Methods ---

        /// <summary>
        /// Adds a new requirement entry to the collection.
        /// </summary>
        private void ExecuteAddRequirement()
        {
            var newReq = new RequirementViewModel
            {
                RequirementType = RequirementViewModel.AvailableRequirementTypes.First()
            };
            newReq.Validate();
            Requirements.Add(newReq);
            ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Removes the specified requirement entry from the collection.
        /// </summary>
        private void ExecuteRemoveRequirement(object parameter)
        {
            if (parameter is RequirementViewModel requirementToRemove)
            {
                Requirements.Remove(requirementToRemove);
                ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Loads lookup data for control systems and software options.
        /// </summary>
        private async Task LoadLookupsAsync()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var softwareOptionService = scope.ServiceProvider.GetRequiredService<ISoftwareOptionService>();
                var controlSystems = await softwareOptionService.GetControlSystemLookupsAsync();
                if (controlSystems != null)
                {
                    foreach (var cs in controlSystems)
                    {
                        AvailableControlSystems.Add(cs);
                    }
                }

                var allSoftwareOptions = await softwareOptionService.GetAllSoftwareOptionsAsync();
                if (allSoftwareOptions != null)
                {
                    foreach (var so in allSoftwareOptions.OrderBy(s => s.PrimaryName))
                    {
                        AvailableSoftwareOptionsForRequirements.Add(new SoftwareOptionLookupDto { SoftwareOptionId = so.SoftwareOptionId, PrimaryName = so.PrimaryName, ControlSystemId = so.ControlSystemId });
                    }
                }
            }
        }

        /// <summary>
        /// Loads specification code definitions for the selected control system for requirements.
        /// </summary>
        private async Task LoadSpecCodeDefinitionsForRequirementsAsync()
        {
            if (!NewSoftwareOption.ControlSystemId.HasValue || NewSoftwareOption.ControlSystemId.Value <= 0)
            {
                Application.Current.Dispatcher.Invoke(() => { AvailableSpecCodesForRequirements.Clear(); });
                return;
            }
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var softwareOptionService = scope.ServiceProvider.GetRequiredService<ISoftwareOptionService>();
                    var specCodes = await softwareOptionService.GetSpecCodeDefinitionsForControlSystemAsync(NewSoftwareOption.ControlSystemId.Value);
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
                    });
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error loading spec code definitions for requirements: {ex.Message}", "Load Error");
                Application.Current.Dispatcher.Invoke(() => { AvailableSpecCodesForRequirements.Clear(); });
            }
        }

        /// <summary>
        /// Advances the wizard to the next step if validation passes.
        /// </summary>
        private void GoToNextStep()
        {
            if (CanGoToNextStep())
            {

                // If we are about to navigate to the review step (index 4), populate the display text.
                if (CurrentStepIndex == 3)
                {
                    PrepareReviewStepData();
                }
                CurrentStepIndex++;
            }
        }

        /// <summary>
        /// Populates display text for requirements in the review step.
        /// </summary>
        private void PrepareReviewStepData()
        {
            foreach (var req in Requirements)
            {
                switch (req.RequirementType)
                {
                    case "Software Option":
                        var requiredSo = AvailableSoftwareOptionsForRequirements
                            .FirstOrDefault(so => so.SoftwareOptionId == req.RequiredSoftwareOptionId);
                        req.RequiredValueDisplayText = requiredSo?.PrimaryName ?? "Not Found";
                        break;
                    case "Spec Code":
                        var requiredSc = AvailableSpecCodesForRequirements
                            .FirstOrDefault(sc => sc.SpecCodeDefinitionId == req.RequiredSpecCodeDefinitionId);
                        req.RequiredValueDisplayText = requiredSc?.DisplayName ?? "Not Found";
                        break;
                    default:
                        req.RequiredValueDisplayText = req.GeneralRequiredValue;
                        break;
                }
            }
        }

        /// <summary>
        /// Validates the current step before allowing navigation to the next step.
        /// </summary>
        private bool CanGoToNextStep()
        {
            if (IsOnLastStep) return false;

            switch (CurrentStepIndex)
            {
                case 0: // Core Details
                    NewSoftwareOption.Validate();
                    return !NewSoftwareOption.HasErrors;

                case 1: // Option Numbers
                    foreach (var on in OptionNumbers) { on.Validate(); }
                    return OptionNumbers.All(on => !on.HasErrors);

                case 2: // Specification Codes
                    return SpecificationCodes.All(sc =>
                        !string.IsNullOrWhiteSpace(sc.Category) &&
                        !string.IsNullOrWhiteSpace(sc.SpecCodeNo) &&
                        !string.IsNullOrWhiteSpace(sc.SpecCodeBit));

                case 3: // Requirements
                    foreach (var req in Requirements) { req.Validate(); }
                    return Requirements.All(req => !req.HasErrors);

                default:
                    return true;
            }
        }

        /// <summary>
        /// Moves the wizard to the previous step.
        /// </summary>
        private void GoToPreviousStep()
        {
            CurrentStepIndex--;
        }

        /// <summary>
        /// Finalizes and saves the new software option after validation.
        /// </summary>
        private async Task ExecuteSaveAsync()
        {
            // Map ViewModel data to DTOs
            PrepareSaveData();

            // Run final validation
            NewSoftwareOption.Validate();

            if (NewSoftwareOption.HasErrors || OptionNumbers.Any(on => on.HasErrors) || Requirements.Any(req => req.HasErrors))
            {
                _notificationService.ShowWarning("Please fix all validation errors before finishing.", "Validation Failed");
                return;
            }

            var currentUser = _authStateProvider.CurrentUser;
            if (currentUser == null)
            {
                _notificationService.ShowError("Authentication error. Cannot save option.", "Save Error");
                return;
            }

            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var softwareOptionService = scope.ServiceProvider.GetRequiredService<ISoftwareOptionService>();
                    var createdOption = await softwareOptionService.CreateSoftwareOptionAsync(NewSoftwareOption, currentUser.UserId, currentUser.UserName);

                    if (createdOption != null)
                    {
                        _notificationService.ShowSuccess($"Software Option '{createdOption.PrimaryName}' created successfully.", "Save Successful");
                        DialogHost.CloseDialogCommand.Execute(true, null);
                    }
                    else
                    {
                        _notificationService.ShowError("Failed to create Software Option. See notifications for details.", "Save Failed");
                    }
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"An unexpected error occurred while saving: {ex.Message}", "Save Error");
            }
        }

        /// <summary>
        /// Cancels the wizard and closes the dialog.
        /// </summary>
        private void ExecuteCancel()
        {
            DialogHost.CloseDialogCommand.Execute(false, null);
        }
    }
}