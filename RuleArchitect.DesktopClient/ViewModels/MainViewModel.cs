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

        public MainViewModel(IAuthenticationStateProvider authStateProvider, IServiceProvider serviceProvider, SnackbarMessageQueue snackbarMessageQueue)
        {
            _authStateProvider = authStateProvider;
            _serviceProvider = serviceProvider;
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
                    TargetViewModelType = targetVmType,
                    // **UPDATED**: This now correctly assigns the icon.
                    IconKind = icon,
                    NavigateCommand = new RelayCommand(() => NavigateTo(targetVmType))
                });
            }

            switch (CurrentUser.Role)
            {
                case "Administrator":
                    AddNavItem("Dashboard", typeof(AdminDashboardViewModel), PackIconKind.ViewDashboardOutline);
                    AddNavItem("Rulesheets", typeof(SoftwareOptionsViewModel), PackIconKind.FileDocumentMultipleOutline);
                    AddNavItem("Manage Users", typeof(UserManagementViewModel), PackIconKind.AccountGroupOutline);
                    AddNavItem("Activity Log", typeof(UserActivityLogViewModel), PackIconKind.History);
                    break;
            }
        }

        private void NavigateTo(Type viewModelType)
        {
            if (viewModelType == null) return;
            CurrentViewViewModel = (BaseViewModel)_serviceProvider.GetRequiredService(viewModelType);
        }

        private void ExecuteLogout()
        {
            _authStateProvider.ClearCurrentUser();
            LogoutRequested?.Invoke();
        }
    }
}
