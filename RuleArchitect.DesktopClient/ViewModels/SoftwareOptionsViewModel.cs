// In RuleArchitect.DesktopClient/ViewModels/SoftwareOptionsViewModel.cs
using GenesisSentry.DTOs;
using GenesisSentry.Interfaces;
using HeraldKit.Interfaces;
using Microsoft.Extensions.DependencyInjection; // Required for IServiceScopeFactory
using RuleArchitect.ApplicationLogic.DTOs;
using RuleArchitect.ApplicationLogic.Interfaces;
using RuleArchitect.DesktopClient.Commands;
using System; // For Exception and ArgumentNullException
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
// No need for System.Linq here if not used directly

namespace RuleArchitect.DesktopClient.ViewModels
{
    public class SoftwareOptionsViewModel : BaseViewModel
    {
        private readonly IServiceScopeFactory _scopeFactory; // Use this to create scopes
        private readonly IAuthenticationStateProvider _authStateProvider;
        private bool _isLoading;
        private SoftwareOptionDto? _selectedSoftwareOption;
        private UserDto? _currentUser;

        private EditSoftwareOptionViewModel? _currentEditSoftwareOption;
        public EditSoftwareOptionViewModel CurrentEditSoftwareOption
        {
            get => _currentEditSoftwareOption;
            set => SetProperty(ref _currentEditSoftwareOption, value);
        }

        private bool _isDetailPaneVisible;
        public GridLength SplitterColumnEffectiveWidth => IsDetailPaneVisible ? GridLength.Auto : GridLength.Auto;
        public bool IsDetailPaneVisible
        {
            get => _isDetailPaneVisible;
            set
            {
                if (SetProperty(ref _isDetailPaneVisible, value)) // This calls OnPropertyChanged("IsDetailPaneVisible")
                {
                    // This is the crucial line for the column width
                    //OnPropertyChanged(nameof(DetailPaneWidth));
                    //OnPropertyChanged(nameof(SplitterColumnEffectiveWidth));

                    if (!_isDetailPaneVisible)
                    {
                        CurrentEditSoftwareOption = null;
                        IsAdding = false;
                    }
                    UpdateCommandStates(); // Also good to update command states here
                }
            }
        }

        //public GridLength DetailPaneWidth => IsDetailPaneVisible ? new GridLength(1.2, GridUnitType.Star) : new GridLength(0,GridUnitType.Pixel);
        public GridLength DetailPaneWidth => IsDetailPaneVisible ? new GridLength(1.2, GridUnitType.Star) : GridLength.Auto;
        // The IsDetailPaneVisible setter still calls OnPropertyChanged(nameof(DetailPaneWidth))
        // Choose a pixel width like 350, 400, 450 etc. that suits your content.
        //public GridLength DetailPaneWidth => IsDetailPaneVisible ? new GridLength(1.2, GridUnitType.Star) : new GridLength(0, GridUnitType.Pixel); // Or GridUnitType.Auto

        public ObservableCollection<SoftwareOptionDto> SoftwareOptions { get; private set; }

        public SoftwareOptionDto? SelectedSoftwareOption
        {
            get => _selectedSoftwareOption;
            set
            {
                if (SetProperty(ref _selectedSoftwareOption, value)) // SetProperty returns true if value actually changed
                {
                    UpdateCommandStates();
                    // If the new value (now _selectedSoftwareOption) is null, and we are not in 'Add' mode, 
                    // then hide the detail pane.
                    if (_selectedSoftwareOption == null && !IsAdding)
                    {
                        IsDetailPaneVisible = false;
                    }
                    // If you want the detail pane to ALWAYS close when selection changes (and not adding),
                    // you might simplify this further or handle it in the EditCommand logic.
                    // The current PrepareEditSoftwareOption will show it.
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value, UpdateCommandStates);
            //{
            //    if (_isLoading != value)
            //    {
            //        _isLoading = value;
            //        OnPropertyChanged();
            //        // Also notify CanExecute changes for commands that depend on IsLoading
            //        ((RelayCommand)LoadCommand).RaiseCanExecuteChanged();
            //        ((RelayCommand?)AddCommand)?.RaiseCanExecuteChanged();
            //        ((RelayCommand?)EditCommand)?.RaiseCanExecuteChanged();
            //        ((RelayCommand?)DeleteCommand)?.RaiseCanExecuteChanged();
            //    }
            //}
        }

        private bool _isAdding;
        public bool IsAdding
        {
            get => _isAdding;
            set => SetProperty(ref _isAdding, value, UpdateCommandStates);
        }

        public UserDto? CurrentUser
        {
            get => _currentUser;
            set { SetProperty(ref _currentUser, value); }
                
        }

        public ICommand LoadCommand { get; }
        public ICommand? AddCommand { get; private set; }
        public ICommand? EditCommand { get; private set; }
        public ICommand? DeleteCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand CancelEditCommand { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftwareOptionsViewModel"/> class.
        /// This constructor is intended for use by the XAML designer.
        /// </summary>
        //public SoftwareOptionsViewModel()
        //{
        //    // This constructor is primarily for the XAML designer.
        //    // _scopeFactory will be null. Operations requiring it won't work.
        //    _scopeFactory = null!; // Should not be used if this constructor is hit at runtime in error
        //    SoftwareOptions = new ObservableCollection<SoftwareOptionDto>();

        //    LoadCommand = new RelayCommand(async () => await LoadSoftwareOptionsAsync(), () => !IsLoading && _scopeFactory != null);
        //    AddCommand = new RelayCommand(async () => await AddSoftwareOptionAsync(), () => !IsLoading && _scopeFactory != null);
        //    EditCommand = new RelayCommand(async () => await EditSoftwareOptionAsync(), () => SelectedSoftwareOption != null && !IsLoading && _scopeFactory != null);
        //    DeleteCommand = new RelayCommand(async () => await DeleteSoftwareOptionAsync(), () => SelectedSoftwareOption != null && !IsLoading && _scopeFactory != null);

        //    if (DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
        //    {
        //        SoftwareOptions.Add(new SoftwareOptionDto { SoftwareOptionId = 1, PrimaryName = "Sample Option 1 (Design)", Version = 1 });
        //        SoftwareOptions.Add(new SoftwareOptionDto { SoftwareOptionId = 2, PrimaryName = "Sample Option 2 (Design)", Version = 2 });
        //    }
        //}

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftwareOptionsViewModel"/> class with dependencies.
        /// </summary>
        /// <param name="scopeFactory">The service scope factory for creating operational scopes.</param>
        public SoftwareOptionsViewModel(IServiceScopeFactory scopeFactory, IAuthenticationStateProvider authStateProvider)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _authStateProvider = authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));
            SoftwareOptions = new ObservableCollection<SoftwareOptionDto>();

            LoadCommand = new RelayCommand(async () => await LoadSoftwareOptionsAsync(), () => !IsLoading);
            AddCommand = new RelayCommand(PrepareAddSoftwareOption, () => !IsLoading && !IsDetailPaneVisible); // Enable if not already adding/editing
            EditCommand = new RelayCommand(PrepareEditSoftwareOption, () => SelectedSoftwareOption != null && !IsLoading && !IsDetailPaneVisible);
            DeleteCommand = new RelayCommand(async () => await DeleteSoftwareOptionAsync(), () => SelectedSoftwareOption != null && !IsLoading && !IsDetailPaneVisible);

            SaveCommand = new RelayCommand(async () => await SaveSoftwareOptionAsync(), () => IsDetailPaneVisible && CurrentEditSoftwareOption != null && !IsLoading);
            CancelEditCommand = new RelayCommand(CancelEditOrAdd, () => IsDetailPaneVisible);


            CurrentUser = _authStateProvider.CurrentUser;
            _authStateProvider.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(IAuthenticationStateProvider.CurrentUser))
                {
                    CurrentUser = _authStateProvider.CurrentUser;
                }
            };
        }

        // Call this method in setters of IsLoading, SelectedSoftwareOption, IsDetailPaneVisible, etc.
        private void UpdateCommandStates()
        {
            ((RelayCommand)LoadCommand).RaiseCanExecuteChanged();
            ((RelayCommand)AddCommand).RaiseCanExecuteChanged();
            ((RelayCommand)EditCommand).RaiseCanExecuteChanged();
            ((RelayCommand)DeleteCommand).RaiseCanExecuteChanged();
            ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
            ((RelayCommand)CancelEditCommand).RaiseCanExecuteChanged();
        }

        //private async Task LoadSoftwareOptionsAsync()
        //{
        //    if (IsLoading) return;
        //    // Guard for design-time or if scopeFactory wasn't injected (shouldn't happen at runtime with DI)
        //    if (_scopeFactory == null)
        //    {
        //        Console.WriteLine("IServiceScopeFactory is not available. Cannot load options.");
        //        if (DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
        //        {
        //            SoftwareOptions.Clear();
        //            SoftwareOptions.Add(new SoftwareOptionDto { PrimaryName = "Design Mode: Scope factory unavailable" });
        //        }
        //        return;
        //    }

        //    IsLoading = true;
        //    SoftwareOptions.Clear();

        //    try
        //    {
        //        using (var scope = _scopeFactory.CreateScope()) // Create a new scope for this operation
        //        {
        //            var softwareOptionService = scope.ServiceProvider.GetRequiredService<ISoftwareOptionService>();
        //            var options = await softwareOptionService.GetAllSoftwareOptionsAsync();

        //            if (options != null)
        //            {
        //                foreach (var optionEntity in options)
        //                {
        //                    var dto = new SoftwareOptionDto
        //                    {
        //                        SoftwareOptionId = optionEntity.SoftwareOptionId,
        //                        PrimaryName = optionEntity.PrimaryName,
        //                        AlternativeNames = optionEntity.AlternativeNames,
        //                        SourceFileName = optionEntity.SourceFileName,
        //                        PrimaryOptionNumberDisplay = optionEntity.PrimaryOptionNumberDisplay,
        //                        Notes = optionEntity.Notes,
        //                        ControlSystemId = optionEntity.ControlSystemId.GetValueOrDefault(),
        //                        ControlSystemName = optionEntity.ControlSystem?.Name,
        //                        Version = optionEntity.Version,
        //                        LastModifiedDate = optionEntity.LastModifiedDate,
        //                        LastModifiedBy = optionEntity.LastModifiedBy
        //                    };
        //                    SoftwareOptions.Add(dto);
        //                }
        //            }
        //        } // Scope (and DbContext + Service instance for this scope) is disposed here
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error loading software options: {ex.Message}");
        //        // Handle user-facing error display
        //    }
        //    finally
        //    {
        //        IsLoading = false;
        //    }
        //}

        private async Task LoadSoftwareOptionsAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            IsDetailPaneVisible = false; // Hide detail pane during load
            SoftwareOptions.Clear();

            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var softwareOptionService = scope.ServiceProvider.GetRequiredService<ISoftwareOptionService>();
                    var options = await softwareOptionService.GetAllSoftwareOptionsAsync();

                    if (options != null)
                    {
                        foreach (var optionEntity in options.OrderBy(o => o.PrimaryName)) // Example ordering
                        {
                            SoftwareOptions.Add(new SoftwareOptionDto // Map to DTO
                            {
                                SoftwareOptionId = optionEntity.SoftwareOptionId,
                                PrimaryName = optionEntity.PrimaryName,
                                AlternativeNames = optionEntity.AlternativeNames,
                                SourceFileName = optionEntity.SourceFileName,
                                PrimaryOptionNumberDisplay = optionEntity.PrimaryOptionNumberDisplay,
                                Notes = optionEntity.Notes,
                                ControlSystemId = optionEntity.ControlSystemId.GetValueOrDefault(),
                                ControlSystemName = optionEntity.ControlSystem?.Name, // Assuming ControlSystem is loaded
                                Version = optionEntity.Version,
                                LastModifiedDate = optionEntity.LastModifiedDate,
                                LastModifiedBy = optionEntity.LastModifiedBy
                            });
                        }
                    }
                }
                // Consider adding a notification via INotificationService
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading software options: {ex.Message}");
                // Handle user-facing error display via INotificationService
                using (var scope = _scopeFactory.CreateScope())
                {
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    notificationService.ShowError($"Error loading software options: {ex.Message}", "Load Failed");
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Similarly update AddSoftwareOptionAsync, EditSoftwareOptionAsync, DeleteSoftwareOptionAsync
        // to use the _scopeFactory to resolve ISoftwareOptionService within a using(var scope = ...) block.

        //private async Task AddSoftwareOptionAsync()
        //{
        //    if (_scopeFactory == null) return;
        //    Console.WriteLine("AddSoftwareOptionAsync called - (Not Implemented with Scoping yet)");
        //    // Example structure:
        //    // using (var scope = _scopeFactory.CreateScope())
        //    // {
        //    //     var service = scope.ServiceProvider.GetRequiredService<ISoftwareOptionService>();
        //    //     // ... use service to add option ...
        //    // }
        //    // await LoadSoftwareOptionsAsync(); // Refresh list
        //    await Task.CompletedTask;
        //}

        private void PrepareAddSoftwareOption()
        {
            IsAdding = true; // Set flag
            // Resolve EditSoftwareOptionViewModel using _scopeFactory to ensure its dependencies are injected if it had any
            // For now, assuming EditSoftwareOptionViewModel can be newed up directly if it has no complex dependencies.
            // If EditSoftwareOptionViewModel needs ISoftwareOptionService for lookups, it should be resolved via DI.
            using (var scope = _scopeFactory.CreateScope())
            {
                // If EditSoftwareOptionViewModel takes services in its constructor, resolve it:
                CurrentEditSoftwareOption = scope.ServiceProvider.GetRequiredService<EditSoftwareOptionViewModel>();
                // For a simple DTO-like Edit VM:
                //CurrentEditSoftwareOption = new EditSoftwareOptionViewModel(scope.ServiceProvider.GetRequiredService<ISoftwareOptionService>(), _authStateProvider);
                // If you want to pass a new DTO or set it to a "new" state:
                // CurrentEditSoftwareOption.LoadForCreate(); // A method you might add to EditSoftwareOptionViewModel
            }
            IsDetailPaneVisible = true;
            UpdateCommandStates();
            //Application.Current.Dispatcher.Invoke(() => ContentHostGrid.UpdateLayout());
        }
        //private async Task EditSoftwareOptionAsync()
        //{
        //    if (_scopeFactory == null || SelectedSoftwareOption == null) return;
        //    Console.WriteLine($"EditSoftwareOptionAsync called for {SelectedSoftwareOption.PrimaryName} - (Not Implemented with Scoping yet)");
        //    await Task.CompletedTask;
        //}
        //private async Task DeleteSoftwareOptionAsync()
        //{
        //    if (_scopeFactory == null || SelectedSoftwareOption == null) return;
        //    Console.WriteLine($"DeleteSoftwareOptionAsync called for {SelectedSoftwareOption.PrimaryName} - (Not Implemented with Scoping yet)");
        //    await Task.CompletedTask;
        //}

        private async void PrepareEditSoftwareOption()
        {
            if (SelectedSoftwareOption == null) return;
            IsAdding = false; // Clear flag

            using (var scope = _scopeFactory.CreateScope())
            {
                // Assuming EditSoftwareOptionViewModel can take ISoftwareOptionService, etc.
                CurrentEditSoftwareOption = new EditSoftwareOptionViewModel(
                    scope.ServiceProvider.GetRequiredService<ISoftwareOptionService>(),
                    _authStateProvider
                );
                await CurrentEditSoftwareOption.LoadSoftwareOptionAsync(SelectedSoftwareOption.SoftwareOptionId);
            }
            IsDetailPaneVisible = true;
            UpdateCommandStates();
        }

        private async Task SaveSoftwareOptionAsync()
        {
            if (CurrentEditSoftwareOption == null || IsLoading) return;
            // ... existing validation ...

            IsLoading = true;
            try
            {
                // CurrentEditSoftwareOption.ExecuteSaveAsync() now returns a bool
                bool success = await CurrentEditSoftwareOption.ExecuteSaveAsync();

                if (success)
                {
                    // Notify success (using INotificationService if available)
                    Console.WriteLine("Software Option saved successfully by EditSoftwareOptionViewModel.");
                    //_notificationService.ShowSuccess("Software Option saved successfully.", "Save Successful");

                    IsDetailPaneVisible = false;
                    IsAdding = false; // Reset IsAdding flag
                    await LoadSoftwareOptionsAsync(); // Refresh list
                }
                else
                {
                    // Notify failure (using INotificationService if available)
                    Console.WriteLine("Failed to save Software Option as reported by EditSoftwareOptionViewModel.");
                    //_notificationService.ShowError("Failed to save Software Option.", "Save Failed");
                }
            
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving software option: {ex.ToString()}");
                using (var scope = _scopeFactory.CreateScope())
                {
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    notificationService.ShowError($"Error saving software option: {ex.Message}", "Save Error");
                }
            }
            finally
            {
                IsLoading = false;
            }
        }


        private async Task DeleteSoftwareOptionAsync()
        {
            if (SelectedSoftwareOption == null || IsLoading) return;

            // Confirmation Dialog (example)
            if (MessageBox.Show($"Are you sure you want to delete '{SelectedSoftwareOption.PrimaryName}'?",
                                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                return;
            }

            IsLoading = true;
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var service = scope.ServiceProvider.GetRequiredService<ISoftwareOptionService>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    bool success = await service.DeleteSoftwareOptionAsync(SelectedSoftwareOption.SoftwareOptionId);
                    if (success)
                    {
                        notificationService.ShowSuccess($"Software Option '{SelectedSoftwareOption.PrimaryName}' deleted.", "Delete Successful");
                        SoftwareOptions.Remove(SelectedSoftwareOption); // Remove from local collection
                        SelectedSoftwareOption = null; // Clear selection
                    }
                    else
                    {
                        notificationService.ShowError("Failed to delete software option.", "Delete Failed");
                    }
                }
            }
            catch (Exception ex)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    notificationService.ShowError($"Error deleting software option: {ex.Message}", "Delete Error");
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CancelEditOrAdd()
        {
            IsDetailPaneVisible = false;
            IsAdding = false;
            CurrentEditSoftwareOption = null; // Clear the edit context
            // SelectedSoftwareOption = null; // Optionally clear selection
            UpdateCommandStates();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}