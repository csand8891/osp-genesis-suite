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
        public event Action? LogoutRequested;

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

        /// <summary>
        /// A generic command that can be called from child views to request navigation.
        /// The CommandParameter should be the Type of the ViewModel to navigate to.
        /// </summary>
        public ICommand NavigateCommand { get; }

        public MainViewModel(IAuthenticationStateProvider authStateProvider, IServiceProvider serviceProvider, SnackbarMessageQueue snackbarMessageQueue)
        {
            _authStateProvider = authStateProvider;
            _serviceProvider = serviceProvider;
            SnackbarMessageQueue = snackbarMessageQueue;

            NavigationItems = new ObservableCollection<NavigationItemViewModel>();
            CurrentUser = _authStateProvider.CurrentUser;

            ToggleMenuCommand = new RelayCommand(() => IsMenuOpen = !IsMenuOpen);
            LogoutCommand = new RelayCommand(ExecuteLogout);

            // Command to allow child views to request navigation
            NavigateCommand = new RelayCommand(param =>
            {
                if (param is Type vmType)
                {
                    NavigateTo(vmType);
                }
            });


            if (CurrentUser != null)
            {
                BuildNavigationForRole();
                // Set initial view to the first item in the navigation (e.g., Dashboard)
                var initialVmType = NavigationItems.FirstOrDefault()?.TargetViewModelType;
                if (initialVmType != null)
                {
                    NavigateTo(initialVmType);
                }
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
                    TargetViewModelType = targetVmType,
                    IconKind = icon,
                    // The command for the side navigation remains the same
                    NavigateCommand = new RelayCommand(() => NavigateTo(targetVmType))
                });
            }

            switch (CurrentUser.Role)
            {
                case "Administrator":
                    AddNavItem("Dashboard", typeof(AdminDashboardViewModel), PackIconKind.ViewDashboardOutline);
                    AddNavItem("Rulesheets", typeof(SoftwareOptionsViewModel), PackIconKind.FileDocumentMultipleOutline);
                    AddNavItem("Order Management", typeof(OrderManagementViewModel), PackIconKind.ClipboardListOutline);
                    //AddNavItem("Notifications", typeof(NotificationCenterViewModel), PackIconKind.BellOutline);
                    AddNavItem("Manage Users", typeof(UserManagementViewModel), PackIconKind.AccountGroupOutline);
                    AddNavItem("Activity Log", typeof(UserActivityLogViewModel), PackIconKind.History);
                    break;
                    // Other roles can be added here
            }
        }

        private void NavigateTo(Type viewModelType)
        {
            if (viewModelType == null) return;

            // Prevent navigating to the same view again
            if (CurrentViewViewModel?.GetType() == viewModelType)
            {
                return;
            }

            CurrentViewViewModel = (BaseViewModel)_serviceProvider.GetRequiredService(viewModelType);

            // Update the selected item in the navigation menu to reflect the change
            var newSelectedItem = NavigationItems.FirstOrDefault(nav => nav.TargetViewModelType == viewModelType);
            if (_selectedNavigationItem != newSelectedItem)
            {
                _selectedNavigationItem = newSelectedItem;
                OnPropertyChanged(nameof(SelectedNavigationItem));
            }
        }

        private void ExecuteLogout()
        {
            _authStateProvider.ClearCurrentUser();
            LogoutRequested?.Invoke();
        }
    }
}
