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
    public class AddSoftwareOptionWizardViewModel : BaseViewModel
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly INotificationService _notificationService;
        private readonly IAuthenticationStateProvider _authStateProvider;

        private int _currentStepIndex;
        public int CurrentStepIndex
        {
            get => _currentStepIndex;
            set
            {
                if (SetProperty(ref _currentStepIndex, value))
                {
                    OnPropertyChanged(nameof(StepTitle));
                    OnPropertyChanged(nameof(IsOnFirstStep));
                    OnPropertyChanged(nameof(IsOnLastStep));
                    // Manually re-evaluate commands whenever the step changes.
                    ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)PreviousCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                }
            }
        }

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

        public bool IsOnFirstStep => CurrentStepIndex == 0;
        public bool IsOnLastStep => CurrentStepIndex == 4;

        public CreateSoftwareOptionCommandDto NewSoftwareOption { get; }
        public ObservableCollection<ControlSystemLookupDto> AvailableControlSystems { get; }

        // --- UPDATED: Use the new validatable ViewModel for Option Numbers ---
        public ObservableCollection<OptionNumberWizardViewModel> OptionNumbers { get; }

        // --- Properties for Spec Code Step (using DTO as discussed) ---
        public ObservableCollection<SoftwareOptionSpecificationCodeCreateDto> SpecificationCodes { get; }
        public ObservableCollection<string> AvailableCategories { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableSpecNos { get; } = new ObservableCollection<string>(Enumerable.Range(1, 32).Select(i => i.ToString()));
        public ObservableCollection<string> AvailableSpecBits { get; } = new ObservableCollection<string>(Enumerable.Range(0, 8).Select(i => i.ToString()));

        // --- Properties for Requirements Step ---
        public ObservableCollection<RequirementViewModel> Requirements { get; }
        public ObservableCollection<SoftwareOptionLookupDto> AvailableSoftwareOptionsForRequirements { get; }
        public ObservableCollection<SpecCodeDefinitionLookupDto> AvailableSpecCodesForRequirements { get; }


        public ICommand NextCommand { get; }
        public ICommand PreviousCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        // --- Commands for Option Number Step ---
        public ICommand AddOptionNumberCommand { get; }
        public ICommand RemoveOptionNumberCommand { get; }

        // --- Commands for Spec Code Step ---
        public ICommand AddSpecCodeCommand { get; }
        public ICommand RemoveSpecCodeCommand { get; }

        // --- Commands for Requirements Step ---
        public ICommand AddRequirementCommand { get; }
        public ICommand RemoveRequirementCommand { get; }


        public AddSoftwareOptionWizardViewModel(IServiceScopeFactory scopeFactory, INotificationService notificationService, IAuthenticationStateProvider authStateProvider)
        {
            _scopeFactory = scopeFactory;
            _notificationService = notificationService;
            _authStateProvider = authStateProvider;

            NewSoftwareOption = new CreateSoftwareOptionCommandDto();
            AvailableControlSystems = new ObservableCollection<ControlSystemLookupDto>();

            // UPDATED: Initialize collections with the correct types
            SpecificationCodes = new ObservableCollection<SoftwareOptionSpecificationCodeCreateDto>();
            OptionNumbers = new ObservableCollection<OptionNumberWizardViewModel>();
            Requirements = new ObservableCollection<RequirementViewModel>();

            AvailableSoftwareOptionsForRequirements = new ObservableCollection<SoftwareOptionLookupDto>();
            AvailableSpecCodesForRequirements = new ObservableCollection<SpecCodeDefinitionLookupDto>();

            // UPDATED: Sync observable collections with the DTO model
            SpecificationCodes.CollectionChanged += (s, e) => {
                NewSoftwareOption.SpecificationCodes = SpecificationCodes.ToList();
            };
            OptionNumbers.CollectionChanged += (s, e) => {
                // Map from the ViewModel back to the DTO for saving
                NewSoftwareOption.OptionNumbers = OptionNumbers
                    .Select(vm => new OptionNumberRegistryCreateDto { OptionNumber = vm.OptionNumber })
                    .ToList();
            };
            Requirements.CollectionChanged += (s, e) => {
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
            };

            NextCommand = new RelayCommand(GoToNextStep, CanGoToNextStep);
            PreviousCommand = new RelayCommand(GoToPreviousStep, () => !IsOnFirstStep);
            SaveCommand = new RelayCommand(async () => await ExecuteSaveAsync(), () => IsOnLastStep);
            CancelCommand = new RelayCommand(ExecuteCancel);

            // UPDATED: Commands now take correct parameter types
            AddOptionNumberCommand = new RelayCommand(ExecuteAddOptionNumber);
            RemoveOptionNumberCommand = new RelayCommand(ExecuteRemoveOptionNumber, (param) => param is OptionNumberWizardViewModel);

            AddSpecCodeCommand = new RelayCommand(ExecuteAddSpecCode);
            RemoveSpecCodeCommand = new RelayCommand(ExecuteRemoveSpecCode, (param) => param is SoftwareOptionSpecificationCodeCreateDto);

            AddRequirementCommand = new RelayCommand(ExecuteAddRequirement);
            RemoveRequirementCommand = new RelayCommand(ExecuteRemoveRequirement, (param) => param is RequirementViewModel);

            _ = LoadLookupsAsync();

            this.NewSoftwareOption.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(NewSoftwareOption.ControlSystemId))
                {
                    PopulateAvailableCategories();
                    _ = LoadSpecCodeDefinitionsForRequirementsAsync();
                }
                // When a property changes, re-evaluate if we can move to the next step
                ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
            };
        }

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

        // --- UPDATED: Methods for Option Number Commands ---
        private void ExecuteAddOptionNumber()
        {
            var newOption = new OptionNumberWizardViewModel { OptionNumber = "" };
            newOption.Validate(); // Immediately trigger validation to show the error
            OptionNumbers.Add(newOption);
            ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
        }

        private void ExecuteRemoveOptionNumber(object parameter)
        {
            if (parameter is OptionNumberWizardViewModel optionToRemove)
            {
                OptionNumbers.Remove(optionToRemove);
                ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
            }
        }

        // --- Methods for Spec Code Commands (Unchanged logic, uses DTO) ---
        private void ExecuteAddSpecCode()
        {
            SpecificationCodes.Add(new SoftwareOptionSpecificationCodeCreateDto
            {
                Category = AvailableCategories.FirstOrDefault(),
                SpecCodeNo = "1",
                SpecCodeBit = "0"
            });
            ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
        }

        private void ExecuteRemoveSpecCode(object parameter)
        {
            if (parameter is SoftwareOptionSpecificationCodeCreateDto specCodeToRemove)
            {
                SpecificationCodes.Remove(specCodeToRemove);
                ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
            }
        }

        // --- Methods for Requirement Commands ---
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

        private void ExecuteRemoveRequirement(object parameter)
        {
            if (parameter is RequirementViewModel requirementToRemove)
            {
                Requirements.Remove(requirementToRemove);
                ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
            }
        }

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

        private void GoToNextStep()
        {
            if (CanGoToNextStep())
            {
                CurrentStepIndex++;
            }
        }

        private void PrepareReviewStepData()
        {
            // Populate the display text for each requirement
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

        // --- UPDATED: Final hybrid validation logic ---
        private bool CanGoToNextStep()
        {
            if (IsOnLastStep) return false;

            switch (CurrentStepIndex)
            {
                case 0: // Core Details (Uses INotifyDataErrorInfo on DTO)
                    NewSoftwareOption.Validate();
                    return !NewSoftwareOption.HasErrors;

                case 1: // Option Numbers (Uses INotifyDataErrorInfo on ViewModel)
                    foreach (var on in OptionNumbers) { on.Validate(); }
                    return OptionNumbers.All(on => !on.HasErrors);

                case 2: // Specification Codes (Uses direct, simple check)
                    return SpecificationCodes.All(sc =>
                        !string.IsNullOrWhiteSpace(sc.Category) &&
                        !string.IsNullOrWhiteSpace(sc.SpecCodeNo) &&
                        !string.IsNullOrWhiteSpace(sc.SpecCodeBit));

                case 3: // Requirements (Uses INotifyDataErrorInfo on ViewModel)
                    foreach (var req in Requirements) { req.Validate(); }
                    return Requirements.All(req => !req.HasErrors);

                default:
                    return true;
            }
        }

        private void GoToPreviousStep()
        {
            CurrentStepIndex--;
        }

        private async Task ExecuteSaveAsync()
        {
            // Before saving, run one final validation check on all steps.
            NewSoftwareOption.Validate();
            foreach (var on in OptionNumbers) { on.Validate(); }
            foreach (var req in Requirements) { req.Validate(); }

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
                        _notificationService.ShowError("Failed to create Software Option.", "Save Failed");
                    }
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"An unexpected error occurred while saving: {ex.Message}", "Save Error");
            }
        }

        private void ExecuteCancel()
        {
            DialogHost.CloseDialogCommand.Execute(false, null);
        }
    }
}