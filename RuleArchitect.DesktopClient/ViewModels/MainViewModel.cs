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

        // RE-ADDED: The SnackbarMessageQueue property.
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
                    IsMenuOpen = false;
                }
            }
        }

        private bool _isMenuOpen;
        public bool IsMenuOpen
        {
            get => _isMenuOpen;
            set => SetProperty(ref _isMenuOpen, value);
        }
        public ICommand ToggleMenuCommand { get; }

        public ICommand LogoutCommand { get; }

        // UPDATED: Constructor now requires SnackbarMessageQueue again.
        public MainViewModel(IAuthenticationStateProvider authStateProvider, IServiceProvider serviceProvider, SnackbarMessageQueue snackbarMessageQueue)
        {
            _authStateProvider = authStateProvider;
            _serviceProvider = serviceProvider;
            // RE-ADDED: The SnackbarMessageQueue is now injected and stored.
            SnackbarMessageQueue = snackbarMessageQueue;

            NavigationItems = new ObservableCollection<NavigationItemViewModel>();
            CurrentUser = _authStateProvider.CurrentUser;

            ToggleMenuCommand = new RelayCommand(() => IsMenuOpen = !IsMenuOpen);
            LogoutCommand = new RelayCommand(ExecuteLogout);

            if (CurrentUser != null)
            {
                BuildNavigationForRole();
                SelectedNavigationItem = NavigationItems.FirstOrDefault();
            }
        }

        private void BuildNavigationForRole()
        {
            NavigationItems.Clear();
            if (CurrentUser == null) return;

            void AddNavItem(string displayName, Type targetVmType, PackIconKind icon)
            {
                NavigationItems.Add(new NavigationItemViewModel
                {
                    DisplayName = displayName,
                    Icon = icon, // Assuming NavigationItemViewModel has an Icon property
                    TargetViewModelType = targetVmType,
                    NavigateCommand = new RelayCommand(() => NavigateTo(targetVmType)),
                    
                });
            }

            switch (CurrentUser.Role)
            {
                case "Administrator":
                    AddNavItem("Dashboard", typeof(AdminDashboardViewModel), PackIconKind.ViewDashboardOutline);
                    AddNavItem("Order Management", typeof(OrderManagementViewModel), PackIconKind.ClipboardListOutline);
                    AddNavItem("Rulesheets", typeof(SoftwareOptionsViewModel), PackIconKind.FileDocumentMultipleOutline);
                    AddNavItem("Manage Users", typeof(UserManagementViewModel), PackIconKind.AccountGroupOutline);
                    AddNavItem("Activity Log", typeof(UserActivityLogViewModel), PackIconKind.History);
                    break;
                // Add other roles as needed
            }
        }

        private void NavigateTo(Type viewModelType)
        {
            if (viewModelType == null) return;
            CurrentViewViewModel = (BaseViewModel)_serviceProvider.GetRequiredService(viewModelType);
        }

        private void ExecuteLogout()
        {
            var currentMainWindow = Application.Current.MainWindow;
            _authStateProvider.ClearCurrentUser();
            var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
            if (loginWindow.ShowDialog() == true)
            {
                var newMainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
                var newMainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                newMainWindow.DataContext = newMainViewModel;
                Application.Current.MainWindow = newMainWindow;
                newMainWindow.Show();
                currentMainWindow?.Close();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }
    }
}
