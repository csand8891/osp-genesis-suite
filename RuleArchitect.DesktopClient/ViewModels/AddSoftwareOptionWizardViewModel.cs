// File: RuleArchitect.DesktopClient/ViewModels/AddSoftwareOptionWizardViewModel.cs
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
                    ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
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

        // --- Properties for Option Number Step ---
        public ObservableCollection<OptionNumberRegistryCreateDto> OptionNumbers { get; }
        // REMOVED: The SelectedOptionNumber property is no longer needed.

        // --- Properties for Spec Code Step ---
        public ObservableCollection<SoftwareOptionSpecificationCodeCreateDto> SpecificationCodes { get; }
        // REMOVED: The SelectedSpecificationCode property is no longer needed.

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
            SpecificationCodes = new ObservableCollection<SoftwareOptionSpecificationCodeCreateDto>();
            OptionNumbers = new ObservableCollection<OptionNumberRegistryCreateDto>();
            Requirements = new ObservableCollection<RequirementViewModel>();
            AvailableSoftwareOptionsForRequirements = new ObservableCollection<SoftwareOptionLookupDto>();
            AvailableSpecCodesForRequirements = new ObservableCollection<SpecCodeDefinitionLookupDto>();

            // Sync observable collections with the DTO model
            SpecificationCodes.CollectionChanged += (s, e) => {
                NewSoftwareOption.SpecificationCodes = SpecificationCodes.ToList();
            };
            OptionNumbers.CollectionChanged += (s, e) => {
                NewSoftwareOption.OptionNumbers = OptionNumbers.ToList();
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

            // UPDATED: Commands now take a parameter and don't rely on a SelectedItem property.
            AddOptionNumberCommand = new RelayCommand(ExecuteAddOptionNumber);
            RemoveOptionNumberCommand = new RelayCommand(ExecuteRemoveOptionNumber, (param) => param is OptionNumberRegistryCreateDto);

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

        // --- Methods for Option Number Commands ---
        private void ExecuteAddOptionNumber()
        {
            OptionNumbers.Add(new OptionNumberRegistryCreateDto { OptionNumber = "New Option Number" });
        }

        // UPDATED: Method now accepts a parameter.
        private void ExecuteRemoveOptionNumber(object parameter)
        {
            if (parameter is OptionNumberRegistryCreateDto optionToRemove)
            {
                OptionNumbers.Remove(optionToRemove);
            }
        }

        // --- Methods for Spec Code Commands ---
        private void ExecuteAddSpecCode()
        {
            SpecificationCodes.Add(new SoftwareOptionSpecificationCodeCreateDto
            {
                Category = AvailableCategories.FirstOrDefault(),
                SpecCodeNo = "1",
                SpecCodeBit = "0"
            });
        }

        // UPDATED: Method now accepts a parameter.
        private void ExecuteRemoveSpecCode(object parameter)
        {
            if (parameter is SoftwareOptionSpecificationCodeCreateDto specCodeToRemove)
            {
                SpecificationCodes.Remove(specCodeToRemove);
            }
        }

        // --- Methods for Requirement Commands ---
        private void ExecuteAddRequirement()
        {
            var newReq = new RequirementViewModel
            {
                RequirementType = RequirementViewModel.AvailableRequirementTypes.First()
            };
            Requirements.Add(newReq);
        }

        private void ExecuteRemoveRequirement(object parameter)
        {
            if (parameter is RequirementViewModel requirementToRemove)
            {
                Requirements.Remove(requirementToRemove);
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

        private bool CanGoToNextStep()
        {
            if (IsOnLastStep) return false;

            switch (CurrentStepIndex)
            {
                case 0:
                    return !string.IsNullOrWhiteSpace(NewSoftwareOption.PrimaryName) &&
                           NewSoftwareOption.ControlSystemId.HasValue &&
                           NewSoftwareOption.ControlSystemId > 0;
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