using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using RuleArchitect.Abstractions.DTOs.Auth;
using RuleArchitect.Abstractions.Interfaces;
using RuleArchitect.DesktopClient.Commands;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace RuleArchitect.DesktopClient.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IAuthenticationStateProvider _authStateProvider;
        private readonly IServiceProvider _serviceProvider;

        public SnackbarMessageQueue SnackbarMessageQueue { get; }
        private BaseViewModel _currentViewViewModel;
        public BaseViewModel CurrentViewViewModel
        {
            get => _currentViewViewModel;
            set => SetProperty(ref _currentViewViewModel, value);
        }

        private UserDto _currentUser;
        public UserDto CurrentUser
        {
            get => _currentUser;
            private set
            {
                if (SetProperty(ref _currentUser, value))
                {
                    BuildNavigationForRole();
                }
            }
        }

        public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }

        private NavigationItemViewModel _selectedNavigationItem;
        public NavigationItemViewModel SelectedNavigationItem
        {
            get => _selectedNavigationItem;
            set
            {
                if (SetProperty(ref _selectedNavigationItem, value) && value != null)
                {
                    value.NavigateCommand?.Execute(null);
                    IsMenuOpen = false; // Close the menu on selection
                }
            }
        }

        // --- ADDED FOR DRAWERHOST ---
        private bool _isMenuOpen;
        public bool IsMenuOpen
        {
            get => _isMenuOpen;
            set => SetProperty(ref _isMenuOpen, value);
        }
        public ICommand ToggleMenuCommand { get; }
        // -----------------------------

        public ICommand LogoutCommand { get; }

        public MainViewModel(IAuthenticationStateProvider authStateProvider, IServiceProvider serviceProvider, SnackbarMessageQueue snackbarMessageQueue)
        {
            _authStateProvider = authStateProvider;
            _serviceProvider = serviceProvider;
            SnackbarMessageQueue = snackbarMessageQueue;

            NavigationItems = new ObservableCollection<NavigationItemViewModel>();
            CurrentUser = _authStateProvider.CurrentUser;

            // Initialize new command
            ToggleMenuCommand = new RelayCommand(() => IsMenuOpen = !IsMenuOpen);
            LogoutCommand = new RelayCommand(ExecuteLogout);

            if (NavigationItems.Any())
            {
                SelectedNavigationItem = NavigationItems.First();
            }
            else if (CurrentUser != null)
            {
                // Set initial view to dashboard for Admin
                if (CurrentUser.Role == "Administrator")
                {
                    NavigateTo(typeof(AdminDashboardViewModel));
                    // Also set the SelectedNavigationItem to match the initial view
                    SelectedNavigationItem = NavigationItems.FirstOrDefault(n => n.TargetViewModelType == typeof(AdminDashboardViewModel));
                }
            }
        }

        private void BuildNavigationForRole()
        {
            NavigationItems.Clear();
            if (CurrentUser == null) return;

            void AddNavItem(string displayName, Type targetVmType)
            {
                NavigationItems.Add(new NavigationItemViewModel
                {
                    DisplayName = displayName,
                    TargetViewModelType = targetVmType,
                    NavigateCommand = new RelayCommand(() => NavigateTo(targetVmType))
                });
            }

            switch (CurrentUser.Role)
            {
                case "Administrator":
                    AddNavItem("Dashboard", typeof(AdminDashboardViewModel));
                    AddNavItem("Rulesheets", typeof(SoftwareOptionsViewModel));
                    //AddNavItem("Manage Orders", typeof(OrdersViewModel));
                    AddNavItem("Manage Users", typeof(UserManagementViewModel));
                    AddNavItem("Activity Log", typeof(UserActivityLogViewModel));
                    break;

                    // ... other roles
            }
        }

        private void NavigateTo(Type viewModelType)
        {
            if (viewModelType == null) return;

            try
            {
                CurrentViewViewModel = (BaseViewModel)_serviceProvider.GetRequiredService(viewModelType);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load view for {viewModelType.Name}.\n{ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                CurrentViewViewModel = null;
            }
        }

        private void ExecuteLogout()
        {
            _authStateProvider.ClearCurrentUser();
            Application.Current.MainWindow?.Close();
            var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
            var newLoginResult = loginWindow.ShowDialog();
            if (newLoginResult == true)
            {
                var newMainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
                var newMainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                newMainWindow.DataContext = newMainViewModel;
                Application.Current.MainWindow = newMainWindow;
                newMainWindow.Show();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }
    }
}