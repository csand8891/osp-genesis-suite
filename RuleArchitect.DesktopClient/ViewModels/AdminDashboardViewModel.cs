// Ensure this is the content of RuleArchitect.DesktopClient/ViewModels/AdminDashboardViewModel.cs

using RuleArchitect.Abstractions.Interfaces;
using RuleArchitect.DesktopClient.Commands;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using GenesisSentry.Interfaces;
// No need for Microsoft.Extensions.DependencyInjection here if not using IServiceScopeFactory

namespace RuleArchitect.DesktopClient.ViewModels
{
    public class AdminDashboardViewModel : BaseViewModel
    {
        private readonly ISoftwareOptionService _softwareOptionService;
        private readonly IOrderService _orderService;
        private readonly IUserService _userService;

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

        // ****** ENSURE THIS IsLoading PROPERTY IS EXACTLY AS BELOW ******
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                // Use the SetProperty from BaseViewModel which should also handle ItemChangedCallback
                // and then manually raise CanExecuteChanged for the command.
                if (SetProperty(ref _isLoading, value)) // SetProperty returns true if value changed
                {
                    // Manually notify the command that its CanExecute status might have changed.
                    ((RelayCommand)LoadDashboardDataCommand)?.RaiseCanExecuteChanged();
                    // Also notify other commands that might depend on IsLoading
                    ((RelayCommand)GoToRulesheetsCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand)GoToUserManagementCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand)GoToOrdersCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand)GoToReportsCommand)?.RaiseCanExecuteChanged();
                }
            }
        }
        // *****************************************************************

        public ICommand GoToRulesheetsCommand { get; }
        public ICommand GoToUserManagementCommand { get; }
        public ICommand GoToOrdersCommand { get; }
        public ICommand GoToReportsCommand { get; }
        public ICommand LoadDashboardDataCommand { get; }

        public AdminDashboardViewModel(
            ISoftwareOptionService softwareOptionService,
            IOrderService orderService, IUserService userService)
        {
            _softwareOptionService = softwareOptionService ?? throw new ArgumentNullException(nameof(softwareOptionService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

            // IsLoading is false by default for a bool field.
            LoadDashboardDataCommand = new RelayCommand(async () => await LoadDataAsync(), () => !IsLoading);
            System.Diagnostics.Debug.WriteLine($"AdminDashboardViewModel Constructor: IsLoading is initially {IsLoading}. CanExecute LoadDashboardDataCommand: {LoadDashboardDataCommand.CanExecute(null)}");

            // Assuming other commands should also be disabled while loading
            GoToRulesheetsCommand = new RelayCommand(ExecuteGoToRulesheets, () => !IsLoading);
            GoToUserManagementCommand = new RelayCommand(ExecuteGoToUserManagement, () => !IsLoading);
            GoToOrdersCommand = new RelayCommand(ExecuteGoToOrders, () => !IsLoading);
            GoToReportsCommand = new RelayCommand(ExecuteGoToReports, () => !IsLoading);

            System.Diagnostics.Debug.WriteLine("AdminDashboardViewModel: Constructor END. Data loading should be triggered by view's Loaded event.");
        }

        private async Task LoadDataAsync()
        {
            if (IsLoading) return;

            System.Diagnostics.Debug.WriteLine("AdminDashboardViewModel: LoadDataAsync START.");
            IsLoading = true;

            try
            {
                var allOptions = await _softwareOptionService.GetAllSoftwareOptionsAsync();
                TotalRulesheets = allOptions?.Count ?? 0;
                System.Diagnostics.Debug.WriteLine($"AdminDashboardViewModel: TotalRulesheets = {TotalRulesheets}");

                ActiveUsers = await _userService.GetActiveUserCountAsync();
                OrdersPendingReview = 0;
            }
            catch (System.Exception ex)
            {
                TotalRulesheets = -1;
                ActiveUsers = -1;
                OrdersPendingReview = -1;
                System.Diagnostics.Debug.WriteLine($"CRITICAL ERROR in AdminDashboardViewModel.LoadDataAsync: {ex.ToString()}");
                Application.Current?.Dispatcher?.Invoke(() =>
                    MessageBox.Show($"Error loading dashboard data: {ex.Message}", "Dashboard Error", MessageBoxButton.OK, MessageBoxImage.Error)
                );
            }
            finally
            {
                IsLoading = false;
            }
            System.Diagnostics.Debug.WriteLine("AdminDashboardViewModel: LoadDataAsync END.");
        }

        private void ExecuteGoToRulesheets()
        {
            System.Diagnostics.Debug.WriteLine("Navigate to Rulesheets triggered.");
            // Navigation logic here
        }

        private void ExecuteGoToUserManagement()
        {
            System.Diagnostics.Debug.WriteLine("Navigate to User Management triggered.");
            // Navigation logic here
        }

        private void ExecuteGoToOrders()
        {
            System.Diagnostics.Debug.WriteLine("Navigate to Orders triggered.");
            // Navigation logic here
        }

        private void ExecuteGoToReports()
        {
            System.Diagnostics.Debug.WriteLine("Navigate to Reports triggered.");
            // Navigation logic here
        }
    }
}