// In RuleArchitect.DesktopClient/ViewModels/SoftwareOptionsViewModel.cs
using Microsoft.Extensions.DependencyInjection; // Required for IServiceScopeFactory
using RuleArchitect.ApplicationLogic.DTOs;
using RuleArchitect.ApplicationLogic.Interfaces;
using RuleArchitect.DesktopClient.Commands;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System; // For Exception and ArgumentNullException
using GenesisSentry.Interfaces;
using GenesisSentry.DTOs;
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
                    ((RelayCommand?)EditCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand?)DeleteCommand)?.RaiseCanExecuteChanged();
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
                    // Also notify CanExecute changes for commands that depend on IsLoading
                    ((RelayCommand)LoadCommand).RaiseCanExecuteChanged();
                    ((RelayCommand?)AddCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand?)EditCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand?)DeleteCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public UserDto? CurrentUser
        {
            get => _currentUser;
            set { _currentUser = value; OnPropertyChanged(); }
        }

        public ICommand LoadCommand { get; }
        public ICommand? AddCommand { get; private set; }
        public ICommand? EditCommand { get; private set; }
        public ICommand? DeleteCommand { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftwareOptionsViewModel"/> class.
        /// This constructor is intended for use by the XAML designer.
        /// </summary>
        public SoftwareOptionsViewModel()
        {
            // This constructor is primarily for the XAML designer.
            // _scopeFactory will be null. Operations requiring it won't work.
            _scopeFactory = null!; // Should not be used if this constructor is hit at runtime in error
            SoftwareOptions = new ObservableCollection<SoftwareOptionDto>();

            LoadCommand = new RelayCommand(async () => await LoadSoftwareOptionsAsync(), () => !IsLoading && _scopeFactory != null);
            AddCommand = new RelayCommand(async () => await AddSoftwareOptionAsync(), () => !IsLoading && _scopeFactory != null);
            EditCommand = new RelayCommand(async () => await EditSoftwareOptionAsync(), () => SelectedSoftwareOption != null && !IsLoading && _scopeFactory != null);
            DeleteCommand = new RelayCommand(async () => await DeleteSoftwareOptionAsync(), () => SelectedSoftwareOption != null && !IsLoading && _scopeFactory != null);

            if (DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
            {
                SoftwareOptions.Add(new SoftwareOptionDto { SoftwareOptionId = 1, PrimaryName = "Sample Option 1 (Design)", Version = 1 });
                SoftwareOptions.Add(new SoftwareOptionDto { SoftwareOptionId = 2, PrimaryName = "Sample Option 2 (Design)", Version = 2 });
            }
        }

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
            AddCommand = new RelayCommand(async () => await AddSoftwareOptionAsync(), () => !IsLoading);
            EditCommand = new RelayCommand(async () => await EditSoftwareOptionAsync(), () => SelectedSoftwareOption != null && !IsLoading);
            DeleteCommand = new RelayCommand(async () => await DeleteSoftwareOptionAsync(), () => SelectedSoftwareOption != null && !IsLoading);

            CurrentUser = _authStateProvider.CurrentUser;
            _authStateProvider.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(IAuthenticationStateProvider.CurrentUser))
                {
                    CurrentUser = _authStateProvider.CurrentUser;
                }
            };
        }

        private async Task LoadSoftwareOptionsAsync()
        {
            if (IsLoading) return;
            // Guard for design-time or if scopeFactory wasn't injected (shouldn't happen at runtime with DI)
            if (_scopeFactory == null)
            {
                Console.WriteLine("IServiceScopeFactory is not available. Cannot load options.");
                if (DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
                {
                    SoftwareOptions.Clear();
                    SoftwareOptions.Add(new SoftwareOptionDto { PrimaryName = "Design Mode: Scope factory unavailable" });
                }
                return;
            }

            IsLoading = true;
            SoftwareOptions.Clear();

            try
            {
                using (var scope = _scopeFactory.CreateScope()) // Create a new scope for this operation
                {
                    var softwareOptionService = scope.ServiceProvider.GetRequiredService<ISoftwareOptionService>();
                    var options = await softwareOptionService.GetAllSoftwareOptionsAsync();

                    if (options != null)
                    {
                        foreach (var optionEntity in options)
                        {
                            var dto = new SoftwareOptionDto
                            {
                                SoftwareOptionId = optionEntity.SoftwareOptionId,
                                PrimaryName = optionEntity.PrimaryName,
                                AlternativeNames = optionEntity.AlternativeNames,
                                SourceFileName = optionEntity.SourceFileName,
                                PrimaryOptionNumberDisplay = optionEntity.PrimaryOptionNumberDisplay,
                                Notes = optionEntity.Notes,
                                ControlSystemId = optionEntity.ControlSystemId.GetValueOrDefault(),
                                ControlSystemName = optionEntity.ControlSystem?.Name,
                                Version = optionEntity.Version,
                                LastModifiedDate = optionEntity.LastModifiedDate,
                                LastModifiedBy = optionEntity.LastModifiedBy
                            };
                            SoftwareOptions.Add(dto);
                        }
                    }
                } // Scope (and DbContext + Service instance for this scope) is disposed here
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading software options: {ex.Message}");
                // Handle user-facing error display
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Similarly update AddSoftwareOptionAsync, EditSoftwareOptionAsync, DeleteSoftwareOptionAsync
        // to use the _scopeFactory to resolve ISoftwareOptionService within a using(var scope = ...) block.

        private async Task AddSoftwareOptionAsync()
        {
            if (_scopeFactory == null) return;
            Console.WriteLine("AddSoftwareOptionAsync called - (Not Implemented with Scoping yet)");
            // Example structure:
            // using (var scope = _scopeFactory.CreateScope())
            // {
            //     var service = scope.ServiceProvider.GetRequiredService<ISoftwareOptionService>();
            //     // ... use service to add option ...
            // }
            // await LoadSoftwareOptionsAsync(); // Refresh list
            await Task.CompletedTask;
        }
        private async Task EditSoftwareOptionAsync()
        {
            if (_scopeFactory == null || SelectedSoftwareOption == null) return;
            Console.WriteLine($"EditSoftwareOptionAsync called for {SelectedSoftwareOption.PrimaryName} - (Not Implemented with Scoping yet)");
            await Task.CompletedTask;
        }
        private async Task DeleteSoftwareOptionAsync()
        {
            if (_scopeFactory == null || SelectedSoftwareOption == null) return;
            Console.WriteLine($"DeleteSoftwareOptionAsync called for {SelectedSoftwareOption.PrimaryName} - (Not Implemented with Scoping yet)");
            await Task.CompletedTask;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}