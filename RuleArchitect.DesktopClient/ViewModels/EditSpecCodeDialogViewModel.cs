// In RuleArchitect.DesktopClient/ViewModels/EditSpecCodeDialogViewModel.cs
using Microsoft.Extensions.DependencyInjection;
using RuleArchitect.Abstractions.DTOs;
using RuleArchitect.Abstractions.Interfaces;
using RuleArchitect.DesktopClient.Commands;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static RuleArchitect.DesktopClient.ViewModels.EditSoftwareOptionViewModel;

namespace RuleArchitect.DesktopClient.ViewModels
{
    public class EditSpecCodeDialogViewModel : BaseViewModel
    {
        //private readonly ISoftwareOptionService _softwareOptionService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly int _parentControlSystemId;
        private readonly string _parentControlSystemName;

        private SpecCodeViewModel _specCodeToEdit;
        public SpecCodeViewModel SpecCodeToEdit
        {
            get => _specCodeToEdit;
            set => SetProperty(ref _specCodeToEdit, value);
        }

        // Properties to bind to in the dialog (delegating to SpecCodeToEdit)
        public string Category
        {
            get => SpecCodeToEdit.Category;
            set { if (SpecCodeToEdit.Category != value) { SpecCodeToEdit.Category = value; OnPropertyChanged(); UpdateCheckCommandCanExecute(); } }
        }
        public string SpecCodeNo // This will be bound to the ComboBox's SelectedItem
        {
            get => SpecCodeToEdit.SpecCodeNo;
            set { if (SpecCodeToEdit.SpecCodeNo != value) { SpecCodeToEdit.SpecCodeNo = value; OnPropertyChanged(); UpdateCheckCommandCanExecute(); } }
        }
        public string SpecCodeBit // This will be bound to the ComboBox's SelectedItem
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

        // NEW: Collections for ComboBox ItemsSources
        public ObservableCollection<string> AvailableSpecCodeNos { get; }
        public ObservableCollection<string> AvailableSpecCodeBits { get; }
        public ObservableCollection<string> AvailableCategories { get; }

        public ICommand CheckSpecCodeDefinitionCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<SpecCodeViewModel?>? DialogClosed;
        public Action<bool?>? CloseDialogWindowAction { get; set; }

        public EditSpecCodeDialogViewModel(
            //ISoftwareOptionService softwareOptionService,
            IServiceScopeFactory scopeFactory,
            SpecCodeViewModel specCode,
            string parentControlSystemName,
            int parentControlSystemId,
            ObservableCollection<ActivationRuleLookupDto> availableActivationRules)
        {
            //_softwareOptionService = softwareOptionService;
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory)); // STORED
            SpecCodeToEdit = specCode; // This is the instance being edited
            _parentControlSystemName = parentControlSystemName;
            _parentControlSystemId = parentControlSystemId;
            AvailableActivationRules = availableActivationRules;

            // Initialize new collections
            AvailableSpecCodeNos = new ObservableCollection<string>(Enumerable.Range(1, 32).Select(i => i.ToString()));
            AvailableSpecCodeBits = new ObservableCollection<string>(Enumerable.Range(0, 8).Select(i => i.ToString()));
            AvailableCategories = new ObservableCollection<string>();

            PopulateAvailableCategories();

            CheckSpecCodeDefinitionCommand = new RelayCommand(async () => await ExecuteCheckSpecCodeDefinitionAsync(), CanExecuteCheck);
            SaveCommand = new RelayCommand(ExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);

            IsDescriptionReadOnly = true;
            if (SpecCodeToEdit.SpecCodeDefinitionId > 0 && !string.IsNullOrEmpty(SpecCodeToEdit.Description))
            {
                IsDescriptionReadOnly = true;
            }
            else if (SpecCodeToEdit.SpecCodeDefinitionId == 0)
            {
                IsDescriptionReadOnly = false;
            }
            // If editing an existing item, pre-select values if they are valid, or default
            if (string.IsNullOrEmpty(SpecCodeToEdit.SpecCodeNo) && AvailableSpecCodeNos.Any()) SpecCodeToEdit.SpecCodeNo = AvailableSpecCodeNos.First();
            if (string.IsNullOrEmpty(SpecCodeToEdit.SpecCodeBit) && AvailableSpecCodeBits.Any()) SpecCodeToEdit.SpecCodeBit = AvailableSpecCodeBits.First();

            if (string.IsNullOrEmpty(SpecCodeToEdit.Category) && AvailableCategories.Any())
            {
                Category = AvailableCategories.First(); // Set SpecCodeToEdit.Category, which will update the property
            }
            else if (!string.IsNullOrEmpty(SpecCodeToEdit.Category) && !AvailableCategories.Contains(SpecCodeToEdit.Category))
            {
                // If current category is not in the new list, clear it or set to default.
                // For editable ComboBox, user might want to keep it. For non-editable, clearing is better.
                Category = AvailableCategories.Any() ? AvailableCategories.First() : string.Empty;
            }
        }

        private void PopulateAvailableCategories()
        {
            AvailableCategories.Clear();
            if (!string.IsNullOrEmpty(_parentControlSystemName))
            {
                // Using ToUpperInvariant for case-insensitive "P" check
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
            OnPropertyChanged(nameof(AvailableCategories)); // Notify the UI
        }

        private void UpdateCheckCommandCanExecute()
        {
            ((RelayCommand)CheckSpecCodeDefinitionCommand).RaiseCanExecuteChanged();
        }

        private bool CanExecuteCheck()
        {
            return !string.IsNullOrWhiteSpace(Category) &&
                   !string.IsNullOrWhiteSpace(SpecCodeNo) && // Will be from ComboBox, so should be valid if selected
                   !string.IsNullOrWhiteSpace(SpecCodeBit);  // Will be from ComboBox
        }

        private async Task ExecuteCheckSpecCodeDefinitionAsync()
        {
            if (!CanExecuteCheck()) return;

            // Create a scope and resolve the service here
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
            DialogClosed?.Invoke(SpecCodeToEdit);
            CloseDialogWindowAction?.Invoke(true);
        }

        private void ExecuteCancel()
        {
            DialogClosed?.Invoke(null);
            CloseDialogWindowAction?.Invoke(false);
        }
    }
}