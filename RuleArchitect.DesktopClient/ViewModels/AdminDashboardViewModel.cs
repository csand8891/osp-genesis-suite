// File: RuleArchitect.DesktopClient/ViewModels/AdminDashboardViewModel.cs
using GenesisSentry.Interfaces;
using LiveCharts;
using LiveCharts.Wpf;
using MaterialDesignColors;
using RuleArchitect.Abstractions.DTOs.Activity;
using RuleArchitect.Abstractions.Interfaces;
using RuleArchitect.DesktopClient.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace RuleArchitect.DesktopClient.ViewModels
{
    public class AdminDashboardViewModel : BaseViewModel
    {
        private readonly ISoftwareOptionService _softwareOptionService;
        private readonly IOrderService _orderService;
        private readonly IUserService _userService;
        private readonly IUserActivityLogService _activityLogService;

        // --- Manually Implemented Properties with Backing Fields ---

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

        private SeriesCollection _activitySeries;
        public SeriesCollection ActivitySeries
        {
            get => _activitySeries;
            set => SetProperty(ref _activitySeries, value);
        }

        private string[] _activityLabels;
        public string[] ActivityLabels
        {
            get => _activityLabels;
            private set => SetProperty(ref _activityLabels, value);
        }

        private SeriesCollection _rulesheetDistributionSeries;
        public SeriesCollection RulesheetDistributionSeries
        {
            get => _rulesheetDistributionSeries;
            set => SetProperty(ref _rulesheetDistributionSeries, value);
        }

        private string[] _rulesheetDistributionLabels;
        public string[] RulesheetDistributionLabels
        {
            get => _rulesheetDistributionLabels;
            private set => SetProperty(ref _rulesheetDistributionLabels, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                // Use the SetProperty overload that takes an action to update commands
                SetProperty(ref _isLoading, value, () =>
                {
                    // Manually notify commands that their CanExecute status may have changed
                    (LoadDashboardDataCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (GoToRulesheetsCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (GoToUserManagementCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (GoToOrdersCommand as RelayCommand)?.RaiseCanExecuteChanged();
                });
            }
        }

        public Func<double, string> YFormatter { get; } = value => value.ToString("N0");

        // --- Manually Implemented Commands ---
        public ICommand LoadDashboardDataCommand { get; }
        public ICommand GoToRulesheetsCommand { get; }
        public ICommand GoToUserManagementCommand { get; }
        public ICommand GoToOrdersCommand { get; }
        public ICommand GoToReportsCommand { get; } // From original code

        public AdminDashboardViewModel(
            ISoftwareOptionService softwareOptionService,
            IOrderService orderService, IUserService userService, IUserActivityLogService activityLogService)
        {
            _softwareOptionService = softwareOptionService ?? throw new ArgumentNullException(nameof(softwareOptionService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _activityLogService = activityLogService ?? throw new ArgumentNullException(nameof(activityLogService));

            _activitySeries = new SeriesCollection();
            _activityLabels = Array.Empty<string>();
            _rulesheetDistributionSeries = new SeriesCollection();
            _rulesheetDistributionLabels = Array.Empty<string>();

            // Initialize commands in the constructor using your original RelayCommand class
            LoadDashboardDataCommand = new RelayCommand(async () => await LoadDataAsync(), () => !IsLoading);
            GoToRulesheetsCommand = new RelayCommand(() => ExecuteGoToRulesheets(), () => !IsLoading);
            GoToUserManagementCommand = new RelayCommand(() => ExecuteGoToUserManagement(), () => !IsLoading);
            GoToOrdersCommand = new RelayCommand(() => ExecuteGoToOrders(), () => !IsLoading);
            GoToReportsCommand = new RelayCommand(() => ExecuteGoToReports(), () => !IsLoading);
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                var allOptions = await _softwareOptionService.GetAllSoftwareOptionsAsync();
                TotalRulesheets = allOptions?.Count ?? 0;

                if (allOptions != null && allOptions.Any())
                {
                    var distribution = allOptions
                        .Where(o => !string.IsNullOrEmpty(o.ControlSystemName))
                        .GroupBy(o => o.ControlSystemName)
                        .Select(g => new { ControlSystem = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .ToList();

                    RulesheetDistributionLabels = distribution.Select(d => d.ControlSystem).ToArray();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        RulesheetDistributionSeries.Clear();
                        // Example color palette (use your theme or custom colors)
                        Brush[] pieColors = new Brush[]
                        {
                            (Brush)Application.Current.FindResource("SecondaryHueLightBrush"),
                            (Brush)Application.Current.FindResource("SecondaryHueMidBrush"),
                            (Brush)Application.Current.FindResource("PrimaryHueDarkBrush"),
                            (Brush)Application.Current.FindResource("SecondaryHueDarkBrush"),
                            Brushes.Orange, // fallback
                            Brushes.Green   // fallback
                        };

                        int colorIndex = 0;
                        foreach (var d in distribution)
                        {
                            RulesheetDistributionSeries.Add(new PieSeries
                            {
                                Title = d.ControlSystem,
                                Values = new ChartValues<int> { d.Count },
                                DataLabels = true,
                                Fill = pieColors[colorIndex % pieColors.Length] // Cycle through colors
                            });
                            colorIndex++;
                        }
                    });
                }

                ActiveUsers = await _userService.GetActiveUserCountAsync();
                OrdersPendingReview = 0;

                var sevenDaysAgo = DateTime.UtcNow.AddDays(-7).Date;
                var logs = await _activityLogService.GetActivityLogsAsync(new ActivityLogFilterDto { DateFrom = sevenDaysAgo });
                var activityByDay = logs
                    .GroupBy(l => l.Timestamp.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Date)
                    .ToList();

                var dateRange = Enumerable.Range(0, 7).Select(offset => DateTime.UtcNow.AddDays(-6 + offset).Date);
                var chartData = from date in dateRange
                                join activity in activityByDay on date equals activity.Date into gj
                                from subActivity in gj.DefaultIfEmpty()
                                select new { Label = date.ToString("MMM dd"), Value = subActivity?.Count ?? 0 };

                ActivityLabels = chartData.Select(x => x.Label).ToArray();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ActivitySeries.Clear();
                    ActivitySeries.Add(new LineSeries
                    {
                        Title = "User Activities",
                        Values = new ChartValues<int>(chartData.Select(x => x.Value)),
                        PointGeometry = DefaultGeometries.Circle,
                        PointGeometrySize = 10,
                        Fill = (SolidColorBrush)Application.Current.FindResource("PrimaryHueLightBrush"),
                        Stroke = (SolidColorBrush)Application.Current.FindResource("SecondaryHueLightBrush"),
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard data: {ex.Message}", "Dashboard Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteGoToRulesheets()
        {
            // TODO: Implement navigation logic
        }

        private void ExecuteGoToUserManagement()
        {
            // TODO: Implement navigation logic
        }

        private void ExecuteGoToOrders()
        {
            // TODO: Implement navigation logic
        }

        private void ExecuteGoToReports()
        {
            // TODO: Implement navigation logic
        }
    }
}
