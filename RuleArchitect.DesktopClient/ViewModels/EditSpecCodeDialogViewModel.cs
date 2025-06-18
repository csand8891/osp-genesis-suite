// In RuleArchitect.DesktopClient/ViewModels/EditSpecCodeDialogViewModel.cs
using Microsoft.Extensions.DependencyInjection;
using RuleArchitect.Abstractions.DTOs;
using RuleArchitect.Abstractions.DTOs.Lookups;
using RuleArchitect.Abstractions.Interfaces;
using RuleArchitect.DesktopClient.Commands;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;

namespace RuleArchitect.DesktopClient.ViewModels
{
    public class EditSpecCodeDialogViewModel : BaseViewModel
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly int _parentControlSystemId;
        private readonly string _parentControlSystemName;
        private readonly EditSoftwareOptionViewModel _parentViewModel;

        private SpecCodeViewModel _specCodeToEdit;
        public SpecCodeViewModel SpecCodeToEdit
        {
            get => _specCodeToEdit;
            set => SetProperty(ref _specCodeToEdit, value);
        }

        // Add this property
        public bool IsAddingNew { get; set; }

        // Properties to bind to in the dialog (delegating to SpecCodeToEdit)
        public string Category
        {
            get => SpecCodeToEdit.Category;
            set { if (SpecCodeToEdit.Category != value) { SpecCodeToEdit.Category = value; OnPropertyChanged(); UpdateCheckCommandCanExecute(); } }
        }
        public string SpecCodeNo
        {
            get => SpecCodeToEdit.SpecCodeNo;
            set { if (SpecCodeToEdit.SpecCodeNo != value) { SpecCodeToEdit.SpecCodeNo = value; OnPropertyChanged(); UpdateCheckCommandCanExecute(); } }
        }
        public string SpecCodeBit
        {
            get => SpecCodeToEdit.SpecCodeBit;
            set { if (SpecCodeToEdit.SpecCodeBit != value) { SpecCodeToEdit.SpecCodeBit = value; OnPropertyChanged(); UpdateCheckCommandCanExecute(); } }
        }
        public string? Description
        {
            get => SpecCodeToEdit.Description;
            set { if (SpecCodeToEdit.Description != value) { SpecCodeToEdit.Description = value; OnPropertyChanged(); } }
        }
        public bool IsDescriptionReadOnly
        {
            get => SpecCodeToEdit.IsDescriptionReadOnly;
            set { if (SpecCodeToEdit.IsDescriptionReadOnly != value) { SpecCodeToEdit.IsDescriptionReadOnly = value; OnPropertyChanged(); } }
        }
        public int? SoftwareOptionActivationRuleId
        {
            get => SpecCodeToEdit.SoftwareOptionActivationRuleId;
            set { if (SpecCodeToEdit.SoftwareOptionActivationRuleId != value) { SpecCodeToEdit.SoftwareOptionActivationRuleId = value; OnPropertyChanged(); } }
        }
        public string? SpecificInterpretation
        {
            get => SpecCodeToEdit.SpecificInterpretation;
            set { if (SpecCodeToEdit.SpecificInterpretation != value) { SpecCodeToEdit.SpecificInterpretation = value; OnPropertyChanged(); } }
        }

        public ObservableCollection<ActivationRuleLookupDto> AvailableActivationRules { get; }
        public ObservableCollection<string> AvailableSpecCodeNos { get; }
        public ObservableCollection<string> AvailableSpecCodeBits { get; }
        public ObservableCollection<string> AvailableCategories { get; }

        public ICommand CheckSpecCodeDefinitionCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand CreateNewActivationRuleCommand { get; }

        public EditSpecCodeDialogViewModel(
            IServiceScopeFactory scopeFactory,
            SpecCodeViewModel specCode,
            string parentControlSystemName,
            int parentControlSystemId,
            EditSoftwareOptionViewModel parentViewModel)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _parentViewModel = parentViewModel ?? throw new ArgumentNullException(nameof(parentViewModel));
            SpecCodeToEdit = specCode;
            _parentControlSystemName = parentControlSystemName;
            _parentControlSystemId = parentControlSystemId;

            AvailableSpecCodeNos = new ObservableCollection<string>(Enumerable.Range(1, 32).Select(i => i.ToString()));
            AvailableSpecCodeBits = new ObservableCollection<string>(Enumerable.Range(0, 8).Select(i => i.ToString()));
            AvailableCategories = new ObservableCollection<string>();
            AvailableActivationRules = _parentViewModel.AvailableActivationRules;

            PopulateAvailableCategories();

            CheckSpecCodeDefinitionCommand = new RelayCommand(async () => await ExecuteCheckSpecCodeDefinitionAsync(), CanExecuteCheck);
            SaveCommand = new RelayCommand(ExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);
            CreateNewActivationRuleCommand = new RelayCommand(ExecuteCreateNewActivationRule);

            IsDescriptionReadOnly = true;
            if (SpecCodeToEdit.SpecCodeDefinitionId > 0 && !string.IsNullOrEmpty(SpecCodeToEdit.Description))
            {
                IsDescriptionReadOnly = true;
            }
            else if (SpecCodeToEdit.SpecCodeDefinitionId == 0)
            {
                IsDescriptionReadOnly = false;
            }

            if (string.IsNullOrEmpty(SpecCodeToEdit.SpecCodeNo) && AvailableSpecCodeNos.Any()) SpecCodeToEdit.SpecCodeNo = AvailableSpecCodeNos.First();
            if (string.IsNullOrEmpty(SpecCodeToEdit.SpecCodeBit) && AvailableSpecCodeBits.Any()) SpecCodeToEdit.SpecCodeBit = AvailableSpecCodeBits.First();

            if (string.IsNullOrEmpty(SpecCodeToEdit.Category) && AvailableCategories.Any())
            {
                Category = AvailableCategories.First();
            }
            else if (!string.IsNullOrEmpty(SpecCodeToEdit.Category) && !AvailableCategories.Contains(SpecCodeToEdit.Category))
            {
                Category = AvailableCategories.Any() ? AvailableCategories.First() : string.Empty;
            }
        }

        private void PopulateAvailableCategories()
        {
            AvailableCategories.Clear();
            if (!string.IsNullOrEmpty(_parentControlSystemName))
            {
                if (_parentControlSystemName.ToUpperInvariant().StartsWith("P"))
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
            OnPropertyChanged(nameof(AvailableCategories));
        }

        private void UpdateCheckCommandCanExecute()
        {
            ((RelayCommand)CheckSpecCodeDefinitionCommand).RaiseCanExecuteChanged();
        }

        private bool CanExecuteCheck()
        {
            return !string.IsNullOrWhiteSpace(Category) &&
                   !string.IsNullOrWhiteSpace(SpecCodeNo) &&
                   !string.IsNullOrWhiteSpace(SpecCodeBit);
        }

        private async Task ExecuteCheckSpecCodeDefinitionAsync()
        {
            if (!CanExecuteCheck()) return;

            using (var scope = _scopeFactory.CreateScope())
            {
                var softwareOptionService = scope.ServiceProvider.GetRequiredService<ISoftwareOptionService>();
                var foundSpecCodeDef = await softwareOptionService.FindSpecCodeDefinitionAsync(
                    _parentControlSystemId, Category, SpecCodeNo, SpecCodeBit);

                if (foundSpecCodeDef != null)
                {
                    Description = foundSpecCodeDef.Description;
                    SpecCodeToEdit.SpecCodeDefinitionId = foundSpecCodeDef.SpecCodeDefinitionId;
                    IsDescriptionReadOnly = true;
                }
                else
                {
                    SpecCodeToEdit.SpecCodeDefinitionId = 0;
                    IsDescriptionReadOnly = false;
                }
            }
        }

        private void ExecuteSave()
        {
            if (string.IsNullOrWhiteSpace(Category) || string.IsNullOrWhiteSpace(SpecCodeNo) || string.IsNullOrWhiteSpace(SpecCodeBit))
            {
                MessageBox.Show("Category, Spec No, and Spec Bit are required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            DialogHost.CloseDialogCommand.Execute(this.SpecCodeToEdit, null);
        }

        private void ExecuteCancel()
        {
            DialogHost.CloseDialogCommand.Execute(null, null);
        }

        private void ExecuteCreateNewActivationRule()
        {
            _parentViewModel.AddActivationRuleCommand.Execute(null);

            var newlyAddedRule = _parentViewModel.ActivationRules.LastOrDefault();
            if (newlyAddedRule != null)
            {
                this.SoftwareOptionActivationRuleId = newlyAddedRule.TempId;
            }
        }
    }
}
