// In RuleArchitect.DesktopClient/ViewModels/AdminDashboardViewModel.cs
using RuleArchitect.ApplicationLogic.Interfaces; // For ISoftwareOptionService, IOrderService, IUserService (you'll need to define IUserService)
using RuleArchitect.DesktopClient.Commands; // For RelayCommand
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
// Add other necessary using statements

namespace RuleArchitect.DesktopClient.ViewModels
{
    public class AdminDashboardViewModel : BaseViewModel // Assuming you have a BaseViewModel for INotifyPropertyChanged
    {
        // Services to be injected
        private readonly ISoftwareOptionService _softwareOptionService;
        private readonly IOrderService _orderService;
        // private readonly IUserService _userService; // You'll need to create this service for user counts etc.
        // private readonly INavigationService _navigationService; // For handling navigation commands

        // Properties for Card Data Bindings
        private int _totalRulesheets;
        public int TotalRulesheets
        {
            get => _totalRulesheets;
            set => SetProperty(ref _totalRulesheets, value);
        }

        private int _activeUsers;
        public int ActiveUsers
        {
            get => _activeUsers;
            set => SetProperty(ref _activeUsers, value);
        }

        private int _ordersPendingReview;
        public int OrdersPendingReview
        {
            get => _ordersPendingReview;
            set => SetProperty(ref _ordersPendingReview, value);
        }

        // Commands for Buttons
        public ICommand GoToRulesheetsCommand { get; }
        public ICommand GoToUserManagementCommand { get; }
        public ICommand GoToOrdersCommand { get; }
        public ICommand GoToReportsCommand { get; }
        public ICommand LoadDashboardDataCommand { get; }


        public AdminDashboardViewModel(
            ISoftwareOptionService softwareOptionService,
            IOrderService orderService
            // IUserService userService, // Inject when created
            // INavigationService navigationService // Inject your navigation service
            )
        {
            _softwareOptionService = softwareOptionService;
            _orderService = orderService;
            // _userService = userService;
            // _navigationService = navigationService;

            // Initialize Commands
            LoadDashboardDataCommand = new RelayCommand(async () => await LoadDataAsync());
            GoToRulesheetsCommand = new RelayCommand(ExecuteGoToRulesheets);
            GoToUserManagementCommand = new RelayCommand(ExecuteGoToUserManagement);
            GoToOrdersCommand = new RelayCommand(ExecuteGoToOrders);
            GoToReportsCommand = new RelayCommand(ExecuteGoToReports);

            // Load data when ViewModel is created
            // Consider if this should be called explicitly by the view or a navigation event
            // Change this for better debugging:
            // _ = LoadDataAsync(); 
            // TO:
            //Task.Run(async () => {
            //    try
            //    {
            //        await LoadDataAsync();
            //    }
            //    catch (Exception ex)
            //    {
            //        System.Diagnostics.Debug.WriteLine($"CRITICAL UNHANDLED EXCEPTION from LoadDataAsync task: {ex.ToString()}");
            //        // You might want to dispatch a message to the UI thread here to show an error
            //        Application.Current?.Dispatcher?.Invoke(() =>
            //            MessageBox.Show($"Critical error during dashboard loading: {ex.Message}", "Dashboard Load Error", MessageBoxButton.OK, MessageBoxImage.Error)
            //        );
            //    }
            //});
            System.Diagnostics.Debug.WriteLine("AdminDashboardViewModel: Constructor END (LoadDataAsync launched).");
        }

        private async Task LoadDataAsync()
        {
            System.Diagnostics.Debug.WriteLine("AdminDashboardViewModel: LoadDataAsync START.");
            try
            {
                var allOptions = await _softwareOptionService.GetAllSoftwareOptionsAsync();
                TotalRulesheets = allOptions?.Count ?? 0;
                System.Diagnostics.Debug.WriteLine($"AdminDashboardViewModel: TotalRulesheets = {TotalRulesheets}");
            }
            catch (System.Exception ex)
            {
                TotalRulesheets = -1;
                System.Diagnostics.Debug.WriteLine($"Error loading rulesheets count in LoadDataAsync: {ex.ToString()}"); // Log full exception
                                                                                                                         // Do not re-throw here if the Task.Run wrapper is meant to catch it. Or, handle specifically.
            }

            ActiveUsers = 0;
            OrdersPendingReview = 0;
            System.Diagnostics.Debug.WriteLine("AdminDashboardViewModel: LoadDataAsync END.");
        }

        private void ExecuteGoToRulesheets()
        {
            // Use NavigationService or other mechanism to navigate to the Rulesheets management view
            // _navigationService.NavigateTo(ViewModelLocator.RulesheetsManagementPageKey);
            System.Diagnostics.Debug.WriteLine("Navigate to Rulesheets triggered.");
            // For now, this could switch the content of your MainWindow or open a new window.
            // This is where your existing SoftwareOptionsViewModel and its view would be shown.
        }

        private void ExecuteGoToUserManagement()
        {
            System.Diagnostics.Debug.WriteLine("Navigate to User Management triggered.");
            // _navigationService.NavigateTo(ViewModelLocator.UserManagementPageKey);
        }

        private void ExecuteGoToOrders()
        {
            System.Diagnostics.Debug.WriteLine("Navigate to Orders triggered.");
            // _navigationService.NavigateTo(ViewModelLocator.OrdersOverviewPageKey);
        }

        private void ExecuteGoToReports()
        {
            System.Diagnostics.Debug.WriteLine("Navigate to Reports triggered.");
            // _navigationService.NavigateTo(ViewModelLocator.ReportsPageKey);
        }
    }
}