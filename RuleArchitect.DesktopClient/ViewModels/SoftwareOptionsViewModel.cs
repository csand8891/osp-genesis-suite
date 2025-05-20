// In RuleArchitect.DesktopClient/ViewModels/SoftwareOptionsViewModel.cs
using RuleArchitect.ApplicationLogic.DTOs;      // For SoftwareOptionDto
using RuleArchitect.ApplicationLogic.Interfaces; // For ISoftwareOptionService
using RuleArchitect.ApplicationLogic.Services;   // For SoftwareOption (the entity from the service)
using RuleArchitect.DesktopClient.Commands;      // For RelayCommand (or your project's command location)
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq; // For .Select mapping
using System;     // For Exception

namespace RuleArchitect.DesktopClient.ViewModels
{
    public class SoftwareOptionsViewModel : INotifyPropertyChanged // Or your BaseViewModel
    {
        private readonly ISoftwareOptionService _softwareOptionService;
        private bool _isLoading;
        private SoftwareOptionDto? _selectedSoftwareOption;

        public ObservableCollection<SoftwareOptionDto> SoftwareOptions { get; private set; }

        public SoftwareOptionDto? SelectedSoftwareOption
        {
            get => _selectedSoftwareOption;
            set
            {
                if (_selectedSoftwareOption != value)
                {
                    _selectedSoftwareOption = value;
                    OnPropertyChanged();
                    // Potentially trigger other actions when selection changes
                    // For example, ((RelayCommand)EditCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand LoadCommand { get; }
        // public ICommand AddCommand { get; }
        // public ICommand EditCommand { get; }
        // public ICommand DeleteCommand { get; }

        public SoftwareOptionsViewModel(ISoftwareOptionService softwareOptionService)
        {
            _softwareOptionService = softwareOptionService ?? throw new ArgumentNullException(nameof(softwareOptionService));
            SoftwareOptions = new ObservableCollection<SoftwareOptionDto>();

            // Initialize Commands
            LoadCommand = new RelayCommand(async () => await LoadSoftwareOptionsAsync(), () => !IsLoading);
            // AddCommand = new RelayCommand(async () => await AddSoftwareOptionAsync(), () => !IsLoading);
            // EditCommand = new RelayCommand(async () => await EditSoftwareOptionAsync(), () => SelectedSoftwareOption != null && !IsLoading);
            // DeleteCommand = new RelayCommand(async () => await DeleteSoftwareOptionAsync(), () => SelectedSoftwareOption != null && !IsLoading);

            // Load data initially if desired
            // Task.Run(async () => await LoadSoftwareOptionsAsync()); // Or call from view's Loaded event
        }

        private async Task LoadSoftwareOptionsAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            SoftwareOptions.Clear(); // Clear existing items

            try
            {
                var options = await _softwareOptionService.GetAllSoftwareOptionsAsync(); // This returns List<SoftwareOption>

                if (options != null)
                {
                    foreach (var optionEntity in options) // optionEntity is of type SoftwareOption
                    {
                        // Manual Mapping from SoftwareOption (Entity) to SoftwareOptionDto
                        var dto = new SoftwareOptionDto
                        {
                            SoftwareOptionId = optionEntity.SoftwareOptionId,
                            PrimaryName = optionEntity.PrimaryName,
                            AlternativeNames = optionEntity.AlternativeNames,
                            SourceFileName = optionEntity.SourceFileName,
                            PrimaryOptionNumberDisplay = optionEntity.PrimaryOptionNumberDisplay,
                            Notes = optionEntity.Notes,
                            ControlSystemId = optionEntity.ControlSystemId.GetValueOrDefault(),
                            ControlSystemName = optionEntity.ControlSystem?.Name, // Assuming ControlSystem is included and has a Name property
                            Version = optionEntity.Version,
                            LastModifiedDate = optionEntity.LastModifiedDate,
                            LastModifiedBy = optionEntity.LastModifiedBy
                            // Map other properties as needed
                        };
                        SoftwareOptions.Add(dto);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log them, show a message to the user)
                Console.WriteLine($"Error loading software options: {ex.Message}");
                // Consider showing a user-friendly error message
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Placeholder for Add/Edit/Delete methods
        // private async Task AddSoftwareOptionAsync() { /* ... call service, refresh list ... */ }
        // private async Task EditSoftwareOptionAsync() { /* ... use SelectedSoftwareOption, call service, refresh item/list ... */ }
        // private async Task DeleteSoftwareOptionAsync() { /* ... use SelectedSoftwareOption, call service, remove from list ... */ }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}