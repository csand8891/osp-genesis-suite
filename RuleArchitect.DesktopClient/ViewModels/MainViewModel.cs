// In RuleArchitect.DesktopClient/ViewModels/MainViewModel.cs
using RuleArchitect.DesktopClient.Commands;
using System.Collections.ObjectModel;
using System.Windows.Input;
using RuleArchitect.Abstractions.Interfaces;
using RuleArchitect.Abstractions.DTOs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Linq;
using MaterialDesignThemes.Wpf; // Required for .FirstOrDefault()

namespace RuleArchitect.DesktopClient.ViewModels
{
    // NavigationItemViewModel class as defined above or in its own file

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
            private set // Make private if only set internally
            {
                if (SetProperty(ref _currentUser, value))
                {
                    BuildNavigationForRole(); // Rebuild navigation when user changes (e.g., on login/logout)
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
                // SetProperty will call OnPropertyChanged
                if (SetProperty(ref _selectedNavigationItem, value) && value?.NavigateCommand != null)
                {
                    // The command itself should handle the navigation
                    if (value.NavigateCommand.CanExecute(null))
                    {
                        value.NavigateCommand.Execute(null);
                    }
                }
            }
        }

        // Individual Navigation Commands (can still be used for explicit buttons if desired)
        // These are now largely superseded by the ListBox and NavigationItems approach for the main nav,
        // but can be useful for specific, always-visible actions or context menus.
        // For simplicity, the direct button bindings in XAML from the previous example
        // can also directly invoke NavigateTo(typeof(SpecificViewModel)) if you prefer.
        // Or, they can be properties here that get their RelayCommand instance from the NavigationItems list.

        public ICommand LogoutCommand { get; }

        public MainViewModel(IAuthenticationStateProvider authStateProvider, IServiceProvider serviceProvider, SnackbarMessageQueue snackbarMessageQueue)
        {
            _authStateProvider = authStateProvider;
            _serviceProvider = serviceProvider;
            SnackbarMessageQueue = snackbarMessageQueue;

            NavigationItems = new ObservableCollection<NavigationItemViewModel>();
            CurrentUser = _authStateProvider.CurrentUser; // This will trigger BuildNavigationForRole

            LogoutCommand = new RelayCommand(ExecuteLogout);

            // Set initial view based on the first available navigation item
            if (NavigationItems.Any())
            {
                // Automatically select and navigate to the first item in the list.
                // This will trigger the SelectedNavigationItem setter.
                SelectedNavigationItem = NavigationItems.First();
            }
            else if (CurrentUser != null) // Fallback if navigation items are empty but user exists
            {
                // Fallback to a default view based on role if NavigationItems is empty for some reason
                switch (CurrentUser.Role)
                {
                    case "Administrator":
                        NavigateTo(typeof(AdminDashboardViewModel));
                        break;
                    // Add other default views for other roles
                    default:
                        // Handle unknown role or no default view
                        break;
                }
            }
        }

        private void BuildNavigationForRole()
        {
            NavigationItems.Clear();
            if (CurrentUser == null) return;

            // Helper to add a navigation item
            void AddNavItem(string displayName, Type targetVmType)
            {
                NavigationItems.Add(new NavigationItemViewModel
                {
                    DisplayName = displayName,
                    TargetViewModelType = targetVmType,
                    NavigateCommand = new RelayCommand(() => NavigateTo(targetVmType))
                });
            }

            // Define navigation items based on role
            switch (CurrentUser.Role)
            {
                case "Administrator":
                    AddNavItem("Dashboard", typeof(AdminDashboardViewModel));
                    AddNavItem("Rulesheets", typeof(SoftwareOptionsViewModel)); // Example
                    AddNavItem("Manage Users", typeof(UserManagementViewModel)); // When UserManagementViewModel exists
                    // AddNavItem("View All Orders", typeof(OrdersViewModel));     // When OrdersViewModel exists
                    // AddNavItem("System Reports", typeof(ReportsViewModel));    // When ReportsViewModel exists
                    break;

                case "OrderReview":
                    // AddNavItem("Review Dashboard", typeof(OrderReviewDashboardViewModel));
                    // AddNavItem("Orders for Review", typeof(OrdersForReviewViewModel));
                    break;

                case "OrderProduction":
                    // AddNavItem("Production Dashboard", typeof(ProductionDashboardViewModel));
                    // AddNavItem("Active Orders", typeof(ProductionOrdersViewModel));
                    break;

                case "ProductionReview":
                    // AddNavItem("QC Dashboard", typeof(QcDashboardViewModel));
                    // AddNavItem("Completed Orders", typeof(CompletedOrdersForQcViewModel));
                    break;
                    // Add other roles and their specific navigation items
            }
        }

        //       private void NavigateTo(Type viewModelType)
        //{
        //    if (viewModelType == null)
        //    {
        //        System.Diagnostics.Debug.WriteLine("MainViewModel.NavigateTo: viewModelType is null.");
        //        return;
        //    }
        //    System.Diagnostics.Debug.WriteLine($"MainViewModel.NavigateTo: Attempting to resolve {viewModelType.Name}.");
        //    try
        //    {
        //        CurrentViewViewModel = (BaseViewModel)_serviceProvider.GetRequiredService(viewModelType); // This creates AdminDashboardViewModel
        //        System.Diagnostics.Debug.WriteLine($"MainViewModel.NavigateTo: {viewModelType.Name} resolved and set as CurrentViewViewModel.");
        //    }
        //    catch (Exception ex)
        //    {
        //        // This catch block is not being hit, as per your last statement.
        //        System.Diagnostics.Debug.WriteLine($"CRITICAL ERROR in MainViewModel.NavigateTo({viewModelType.Name}): {ex.ToString()}");
        //        MessageBox.Show($"Failed to load view for {viewModelType.Name}.\n{ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}

        private void NavigateTo(Type viewModelType)
        {
            if (viewModelType == null)
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel.NavigateTo: viewModelType is null.");
                return;
            }
            System.Diagnostics.Debug.WriteLine($"MainViewModel.NavigateTo: Attempting to resolve {viewModelType.Name}.");
            try
            {
                var vmInstance = (BaseViewModel)_serviceProvider.GetRequiredService(viewModelType);
                System.Diagnostics.Debug.WriteLine($"MainViewModel.NavigateTo: {viewModelType.Name} resolved: {vmInstance != null}");
                CurrentViewViewModel = vmInstance; // This triggers PropertyChanged
                System.Diagnostics.Debug.WriteLine($"MainViewModel.NavigateTo: CurrentViewViewModel set to {CurrentViewViewModel?.GetType().Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CRITICAL ERROR in MainViewModel.NavigateTo({viewModelType.Name}): {ex.ToString()}");
                MessageBox.Show($"Failed to load view for {viewModelType.Name}.\n{ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                CurrentViewViewModel = null; // Explicitly set to null on error
            }
        }
        private void ExecuteLogout()
        {
            _authStateProvider.ClearCurrentUser();

            // Close the current MainWindow
            Application.Current.MainWindow?.Close(); // MainWindow is the shell

            // Show LoginWindow again
            // Resolve a new LoginWindow and show it. App will shutdown if login fails.
            var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();

            // It's important that Application.Current.MainWindow is null or closed before showing login as dialog
            // for the application shutdown logic in App.xaml.cs to work correctly if login is cancelled.
            // If LoginWindow sets itself as Application.Current.MainWindow, that's fine.
            // Otherwise, App.xaml.cs needs to handle the case where MainWindow is closed and Login is reshown.

            // A simple way: The App.xaml.cs handles the main loop.
            // This logout could signal the App class to restart the login flow.
            // For now, let's assume the App.xaml.cs handles shutdown if no window is main after this.
            // Or, more robustly, have an event that App.xaml.cs listens to.

            // Simplest for now:
            var newLoginResult = loginWindow.ShowDialog();
            if (newLoginResult == true)
            {
                // Re-initialize the main window with the new user context
                var newMainViewModel = _serviceProvider.GetRequiredService<MainViewModel>(); // Gets a new MainViewModel, new user role
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