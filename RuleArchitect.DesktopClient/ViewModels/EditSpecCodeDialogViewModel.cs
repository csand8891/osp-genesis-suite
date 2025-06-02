// In RuleArchitect.DesktopClient/ViewModels/EditSpecCodeDialogViewModel.cs
using RuleArchitect.ApplicationLogic.DTOs;
using RuleArchitect.ApplicationLogic.Interfaces;
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
        private readonly ISoftwareOptionService _softwareOptionService;
        private readonly int _parentControlSystemId; // Passed from EditSoftwareOptionViewModel

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
            set { if (SpecCodeToEdit.Category != value) { SpecCodeToEdit.Category = value; OnPropertyChanged(); } }
        }
        public string SpecCodeNo
        {
            get => SpecCodeToEdit.SpecCodeNo;
            set { if (SpecCodeToEdit.SpecCodeNo != value) { SpecCodeToEdit.SpecCodeNo = value; OnPropertyChanged(); } }
        }
        public string SpecCodeBit
        {
            get => SpecCodeToEdit.SpecCodeBit;
            set { if (SpecCodeToEdit.SpecCodeBit != value) { SpecCodeToEdit.SpecCodeBit = value; OnPropertyChanged(); } }
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

        public Action<bool?>? CLoseDialogWindowAction { get; set; } // Action to close the dialog window, if needed
        public ICommand CheckSpecCodeDefinitionCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        // To communicate the result back to the caller
        public event Action<SpecCodeViewModel?>? DialogClosed; // Sends back the edited/new item or null if cancelled
        public Action<bool?>? CloseDialogWindowAction { get; set; }
        public EditSpecCodeDialogViewModel(
            ISoftwareOptionService softwareOptionService,
            SpecCodeViewModel specCode,
            int parentControlSystemId,
            ObservableCollection<ActivationRuleLookupDto> availableActivationRules)
        {
            _softwareOptionService = softwareOptionService;
            SpecCodeToEdit = specCode; // This could be a new or existing SpecCodeViewModel
            _parentControlSystemId = parentControlSystemId;
            AvailableActivationRules = availableActivationRules;

            CheckSpecCodeDefinitionCommand = new RelayCommand(async () => await ExecuteCheckSpecCodeDefinitionAsync(), CanExecuteCheck);
            SaveCommand = new RelayCommand(ExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);

            // If it's an existing item with a known SpecCodeDefinitionId,
            // you might want to pre-set IsDescriptionReadOnly based on its current Description.
            // Or, assume user must always click "Check" to populate/lock description.
            IsDescriptionReadOnly = true; // Default until checked
            if (SpecCodeToEdit.SpecCodeDefinitionId > 0 && !string.IsNullOrEmpty(SpecCodeToEdit.Description))
            {
                IsDescriptionReadOnly = true; // If editing an existing, known one.
            }
            else if (SpecCodeToEdit.SpecCodeDefinitionId == 0) // For a brand new item
            {
                IsDescriptionReadOnly = false; // Allow typing description initially for a new one
            }

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

            var foundSpecCodeDef = await _softwareOptionService.FindSpecCodeDefinitionAsync(
                _parentControlSystemId, Category, SpecCodeNo, SpecCodeBit);

            if (foundSpecCodeDef != null)
            {
                Description = foundSpecCodeDef.Description;
                SpecCodeToEdit.SpecCodeDefinitionId = foundSpecCodeDef.SpecCodeDefinitionId; // Store the ID
                IsDescriptionReadOnly = true;
                // Optionally, update SpecCodeToEdit.SpecCodeDisplayName here
            }
            else
            {
                Description = SpecCodeToEdit.Description; // Keep user's typed description if any, or it's blank
                SpecCodeToEdit.SpecCodeDefinitionId = 0; // Indicate it's a new/unfound SpecCodeDefinition
                IsDescriptionReadOnly = false;
            }
            // Make sure commands re-evaluate if needed
            ((RelayCommand)CheckSpecCodeDefinitionCommand).RaiseCanExecuteChanged();
        }

        private void ExecuteSave()
        {
            // Perform validation if needed
            if (string.IsNullOrWhiteSpace(Category) || string.IsNullOrWhiteSpace(SpecCodeNo) || string.IsNullOrWhiteSpace(SpecCodeBit))
            {
                // Show some error to user (e.g., via a message property or notification service)
                MessageBox.Show("Category, Spec No, and Spec Bit are required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            DialogClosed?.Invoke(SpecCodeToEdit);
            CLoseDialogWindowAction?.Invoke(true);
        }

        private void ExecuteCancel()
        {
            DialogClosed?.Invoke(null); // Null indicates cancellation
            CLoseDialogWindowAction?.Invoke(false); // Close the dialog without saving
        }
    }
}