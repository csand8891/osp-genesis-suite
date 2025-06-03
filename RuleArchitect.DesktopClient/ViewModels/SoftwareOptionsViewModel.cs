// File: RuleArchitect.DesktopClient/ViewModels/SoftwareOptionsViewModel.cs
using GenesisSentry.DTOs;
using GenesisSentry.Interfaces;
using HeraldKit.Interfaces; // For INotificationService
using Microsoft.Extensions.DependencyInjection;
using RuleArchitect.ApplicationLogic.DTOs;
using RuleArchitect.ApplicationLogic.Interfaces;
using RuleArchitect.DesktopClient.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace RuleArchitect.DesktopClient.ViewModels
{
    public class SoftwareOptionsViewModel : BaseViewModel
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IAuthenticationStateProvider _authStateProvider;
        private readonly INotificationService _notificationService; // Added

        private bool _isLoading;
        private SoftwareOptionDto? _selectedSoftwareOption;
        private UserDto? _currentUser;
        private EditSoftwareOptionViewModel? _currentEditSoftwareOption;
        private bool _isDetailPaneVisible;
        private bool _isAdding;

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilteredSoftwareOptionsView?.Refresh();
                }
            }
        }
        public ObservableCollection<ControlSystemLookupDto> AllControlSystemsForFilter { get; }
        private int? _selectedFilterControlSystemId;
        public int? SelectedFilterControlSystemId
        {
            get => _selectedFilterControlSystemId;
            set
            {
                if (SetProperty(ref _selectedFilterControlSystemId, value))
                {
                    FilteredSoftwareOptionsView?.Refresh();
                }
            }
        }

        public EditSoftwareOptionViewModel? CurrentEditSoftwareOption
        {
            get => _currentEditSoftwareOption;
            set => SetProperty(ref _currentEditSoftwareOption, value);
        }

        public event EventHandler? DetailPaneVisibilityChanged;
        public bool IsDetailPaneVisible
        {
            get => _isDetailPaneVisible;
            set
            {
                if (SetProperty(ref _isDetailPaneVisible, value))
                {
                    OnPropertyChanged(nameof(MasterPaneColumnWidth));
                    OnPropertyChanged(nameof(SplitterActualColumnWidth));
                    OnPropertyChanged(nameof(DetailPaneColumnWidth));
                    OnPropertyChanged(nameof(SplitterVisibility));
                    DetailPaneVisibilityChanged?.Invoke(this, EventArgs.Empty);

                    if (!_isDetailPaneVisible)
                    {
                        CurrentEditSoftwareOption = null;
                        IsAdding = false;
                    }
                    UpdateCommandStates();
                }
            }
        }

        public GridLength MasterPaneColumnWidth => new GridLength(1, GridUnitType.Star);
        public GridLength DetailPaneColumnWidth => IsDetailPaneVisible ? new GridLength(1.2, GridUnitType.Star) : new GridLength(0, GridUnitType.Pixel);
        public GridLength SplitterActualColumnWidth => IsDetailPaneVisible ? GridLength.Auto : new GridLength(0);
        public Visibility SplitterVisibility => IsDetailPaneVisible ? Visibility.Visible : Visibility.Collapsed;

        public ObservableCollection<SoftwareOptionDto> SoftwareOptions { get; private set; }
        public ICollectionView FilteredSoftwareOptionsView { get; private set; }

        public SoftwareOptionDto? SelectedSoftwareOption
        {
            get => _selectedSoftwareOption;
            set
            {
                if (SetProperty(ref _selectedSoftwareOption, value))
                {
                    UpdateCommandStates();
                    if (_selectedSoftwareOption == null && !IsAdding)
                    {
                        IsDetailPaneVisible = false;
                    }
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value, UpdateCommandStates);
        }

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
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }

        public SoftwareOptionsViewModel() // Parameterless for XAML Designer
        {
            SoftwareOptions = new ObservableCollection<SoftwareOptionDto>();
            AllControlSystemsForFilter = new ObservableCollection<ControlSystemLookupDto>();
            FilteredSoftwareOptionsView = CollectionViewSource.GetDefaultView(SoftwareOptions);
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                SoftwareOptions.Add(new SoftwareOptionDto { PrimaryName = "Design SO1" });
                AllControlSystemsForFilter.Add(new ControlSystemLookupDto { Name = "Design CS1" });
            }
            LoadCommand = new RelayCommand(() => { });
            AddCommand = new RelayCommand(() => { });
            EditCommand = new RelayCommand(() => { });
            DeleteCommand = new RelayCommand(() => { });
            SaveCommand = new RelayCommand(() => { });
            CancelEditCommand = new RelayCommand(() => { });
        }

        public SoftwareOptionsViewModel(
            IServiceScopeFactory scopeFactory,
            IAuthenticationStateProvider authStateProvider,
            INotificationService notificationService) // Added INotificationService
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _authStateProvider = authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService)); // Store it

            SoftwareOptions = new ObservableCollection<SoftwareOptionDto>();
            AllControlSystemsForFilter = new ObservableCollection<ControlSystemLookupDto>();
            FilteredSoftwareOptionsView = CollectionViewSource.GetDefaultView(SoftwareOptions);
            FilteredSoftwareOptionsView.Filter = ApplyFilterPredicate;

            LoadCommand = new RelayCommand(async () => await LoadSoftwareOptionsAndFiltersAsync(), () => !IsLoading);
            AddCommand = new RelayCommand(PrepareAddSoftwareOption, () => !IsLoading && !IsDetailPaneVisible);
            EditCommand = new RelayCommand(PrepareEditSoftwareOption, () => SelectedSoftwareOption != null && !IsLoading && !IsDetailPaneVisible);
            DeleteCommand = new RelayCommand(async () => await DeleteSoftwareOptionAsync(), () => SelectedSoftwareOption != null && !IsLoading && !IsDetailPaneVisible);
            SaveCommand = new RelayCommand(async () => await SaveSoftwareOptionAsync(), () => IsDetailPaneVisible && CurrentEditSoftwareOption != null && !IsLoading);
            CancelEditCommand = new RelayCommand(CancelEditOrAdd, () => IsDetailPaneVisible);

            CurrentUser = _authStateProvider.CurrentUser;
            _authStateProvider.PropertyChanged += (sender, args) => { if (args.PropertyName == nameof(IAuthenticationStateProvider.CurrentUser)) CurrentUser = _authStateProvider.CurrentUser; };
        }

        private async Task LoadSoftwareOptionsAndFiltersAsync()
        {
            IsLoading = true;
            IsDetailPaneVisible = false;
            try
            {
                await LoadControlSystemsForFilterAsync();
                await LoadSoftwareOptionsAsync();
                Application.Current.Dispatcher.Invoke(() => FilteredSoftwareOptionsView?.Refresh());
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to load initial data: {ex.Message}", "Load Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadControlSystemsForFilterAsync()
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var softwareOptionService = scope.ServiceProvider.GetRequiredService<ISoftwareOptionService>();
                    var controlSystems = await softwareOptionService.GetControlSystemLookupsAsync();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        AllControlSystemsForFilter.Clear();
                        if (controlSystems != null)
                        {
                            foreach (var cs in controlSystems.OrderBy(c => c.Name)) AllControlSystemsForFilter.Add(cs);
                        }
                    });
                    OnPropertyChanged(nameof(AllControlSystemsForFilter));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading control systems for filter: {ex.Message}");
                _notificationService.ShowError($"Failed to load control systems for filter: {ex.Message}", "Filter Load Error");
            }
        }

        private bool ApplyFilterPredicate(object item)
        {
            if (item is SoftwareOptionDto so)
            {
                bool matchesSearch = true;
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    string lowerSearchText = SearchText.ToLowerInvariant();
                    matchesSearch = (so.PrimaryName?.ToLowerInvariant().Contains(lowerSearchText) == true) ||
                                    (so.AlternativeNames?.ToLowerInvariant().Contains(lowerSearchText) == true) ||
                                    (so.ControlSystemName?.ToLowerInvariant().Contains(lowerSearchText) == true) ||
                                    (so.PrimaryOptionNumberDisplay?.ToLowerInvariant().Contains(lowerSearchText) == true);
                }
                bool matchesControlSystem = true;
                if (SelectedFilterControlSystemId.HasValue && SelectedFilterControlSystemId.Value > 0)
                {
                    matchesControlSystem = so.ControlSystemId == SelectedFilterControlSystemId.Value;
                }
                return matchesSearch && matchesControlSystem;
            }
            return false;
        }

        private void UpdateCommandStates()
        {
            ((RelayCommand)LoadCommand).RaiseCanExecuteChanged();
            ((RelayCommand)AddCommand).RaiseCanExecuteChanged();
            ((RelayCommand)EditCommand).RaiseCanExecuteChanged();
            ((RelayCommand)DeleteCommand).RaiseCanExecuteChanged();
            ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
            ((RelayCommand)CancelEditCommand).RaiseCanExecuteChanged();
        }

        private async Task LoadSoftwareOptionsAsync()
        {
            // This method is now part of LoadSoftwareOptionsAndFiltersAsync which handles IsLoading
            // No need to set IsLoading or IsDetailPaneVisible here again.
            var tempSoftwareOptions = new List<SoftwareOptionDto>();
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var softwareOptionService = scope.ServiceProvider.GetRequiredService<ISoftwareOptionService>();
                    var options = await softwareOptionService.GetAllSoftwareOptionsAsync();
                    if (options != null)
                    {
                        foreach (var optionEntity in options.OrderBy(o => o.PrimaryName))
                        {
                            tempSoftwareOptions.Add(new SoftwareOptionDto {
                                SoftwareOptionId = optionEntity.SoftwareOptionId,
                                PrimaryName = optionEntity.PrimaryName,
                                ControlSystemId = optionEntity.ControlSystemId.GetValueOrDefault(),
                                ControlSystemName = optionEntity.ControlSystem?.Name,
                                AlternativeNames = optionEntity.AlternativeNames,
                                SourceFileName = optionEntity.SourceFileName,
                                PrimaryOptionNumberDisplay = optionEntity.PrimaryOptionNumberDisplay,
                                Notes = optionEntity.Notes,                            
                                Version = optionEntity.Version,
                                LastModifiedDate = optionEntity.LastModifiedDate,
                                LastModifiedBy = optionEntity.LastModifiedBy,
                            });
                        }
                    }
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SoftwareOptions.Clear();
                    foreach (var dto in tempSoftwareOptions) SoftwareOptions.Add(dto);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading software options: {ex.Message}");
                _notificationService.ShowError($"Error loading software options: {ex.Message}", "Load Failed");
            }
        }

        private void PrepareAddSoftwareOption()
        {
            IsAdding = true;
            CurrentEditSoftwareOption = new EditSoftwareOptionViewModel(
                _authStateProvider,
                _scopeFactory,
                _notificationService
            );
            IsDetailPaneVisible = true;
            UpdateCommandStates();
        }

        private async void PrepareEditSoftwareOption()
        {
            if (SelectedSoftwareOption == null) return;
            IsAdding = false;
            CurrentEditSoftwareOption = new EditSoftwareOptionViewModel(
                _authStateProvider,
                _scopeFactory,
                _notificationService
            );
            await CurrentEditSoftwareOption.LoadSoftwareOptionAsync(SelectedSoftwareOption.SoftwareOptionId);
            IsDetailPaneVisible = true;
            UpdateCommandStates();
        }

        private async Task SaveSoftwareOptionAsync()
        {
            if (CurrentEditSoftwareOption == null || IsLoading) return;
            IsLoading = true;
            try
            {
                bool success = await CurrentEditSoftwareOption.ExecuteSaveAsync();
                if (success)
                {
                    IsDetailPaneVisible = false;
                    IsAdding = false;
                    await LoadSoftwareOptionsAndFiltersAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving software option via main VM: {ex.ToString()}");
                _notificationService.ShowError($"An unexpected critical error occurred during the save process: {ex.Message}", "Save Error");
            }
            finally { IsLoading = false; }
        }

        private async Task DeleteSoftwareOptionAsync()
        {
            if (SelectedSoftwareOption == null || IsLoading) return;
            if (MessageBox.Show($"Are you sure you want to delete '{SelectedSoftwareOption.PrimaryName}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            { return; }
            IsLoading = true;
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var service = scope.ServiceProvider.GetRequiredService<ISoftwareOptionService>();
                    bool success = await service.DeleteSoftwareOptionAsync(SelectedSoftwareOption.SoftwareOptionId);
                    if (success)
                    {
                        _notificationService.ShowSuccess($"Software Option '{SelectedSoftwareOption.PrimaryName}' deleted.", "Delete Successful");
                        var itemToRemove = SoftwareOptions.FirstOrDefault(so => so.SoftwareOptionId == SelectedSoftwareOption.SoftwareOptionId);
                        if (itemToRemove != null) SoftwareOptions.Remove(itemToRemove);
                    }
                    else { _notificationService.ShowError("Failed to delete software option.", "Delete Failed"); }
                }
            }
            catch (Exception ex) { _notificationService.ShowError($"Error deleting software option: {ex.Message}", "Delete Error"); }
            finally { IsLoading = false; }
        }

        private void CancelEditOrAdd()
        {
            IsDetailPaneVisible = false;
            IsAdding = false;
            CurrentEditSoftwareOption = null;
            UpdateCommandStates();
        }
    }
}