// File: RuleArchitect.DesktopClient/ViewModels/AdminDashboardViewModel.cs
using GenesisSentry.Interfaces;
using HeraldKit.Interfaces;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Win32;
using RuleArchitect.Abstractions.DTOs.Activity;
using RuleArchitect.Abstractions.Interfaces;
using RuleArchitect.DesktopClient.Commands;
using RuleArchitect.DesktopClient.Properties;
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
        private readonly IDatabaseService _databaseService;
        private readonly INotificationService _notificationService;
        private readonly IAuthenticationStateProvider _authStateProvider;

        // --- Properties with Backing Fields ---
        private int _totalRulesheets;
        public int TotalRulesheets { get => _totalRulesheets; set => SetProperty(ref _totalRulesheets, value); }

        private int _activeUsers;
        public int ActiveUsers { get => _activeUsers; set => SetProperty(ref _activeUsers, value); }

        private int _ordersPendingReview;
        public int OrdersPendingReview { get => _ordersPendingReview; set => SetProperty(ref _ordersPendingReview, value); }

        private SeriesCollection _activitySeries;
        public SeriesCollection ActivitySeries { get => _activitySeries; set => SetProperty(ref _activitySeries, value); }

        private string[] _activityLabels;
        public string[] ActivityLabels { get => _activityLabels; private set => SetProperty(ref _activityLabels, value); }

        private SeriesCollection _rulesheetDistributionSeries;
        public SeriesCollection RulesheetDistributionSeries { get => _rulesheetDistributionSeries; set => SetProperty(ref _rulesheetDistributionSeries, value); }

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value, () => { ((RelayCommand)LoadDashboardDataCommand).RaiseCanExecuteChanged(); ((RelayCommand)BackupDatabaseCommand).RaiseCanExecuteChanged(); }); }

        private string _lastBackupDisplay = "Never";
        // **FIXED**: Changed 'private set' to 'set' to make the property writable by the binding engine.
        public string LastBackupDisplay { get => _lastBackupDisplay; set => SetProperty(ref _lastBackupDisplay, value); }

        public Func<double, string> YFormatter { get; } = value => value.ToString("N0");

        // --- Commands ---
        public ICommand LoadDashboardDataCommand { get; }
        public ICommand BackupDatabaseCommand { get; }

        public AdminDashboardViewModel(
            ISoftwareOptionService softwareOptionService,
            IOrderService orderService,
            IUserService userService,
            IUserActivityLogService activityLogService,
            IDatabaseService databaseService,
            INotificationService notificationService,
            IAuthenticationStateProvider authStateProvider)
        {
            _softwareOptionService = softwareOptionService ?? throw new ArgumentNullException(nameof(softwareOptionService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _activityLogService = activityLogService ?? throw new ArgumentNullException(nameof(activityLogService));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _authStateProvider = authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));

            _activitySeries = new SeriesCollection();
            _activityLabels = Array.Empty<string>();
            _rulesheetDistributionSeries = new SeriesCollection();

            LoadDashboardDataCommand = new RelayCommand(async () => await LoadDataAsync(), () => !IsLoading);
            BackupDatabaseCommand = new RelayCommand(async () => await ExecuteBackupDatabaseAsync(), () => !IsLoading);
        }

        private void UpdateLastBackupTime()
        {
            var lastBackupTime = Settings.Default.LastBackupTime;
            if (lastBackupTime != default(DateTime))
            {
                LastBackupDisplay = lastBackupTime.ToString("g"); // General short date/time
            }
            else
            {
                LastBackupDisplay = "Never";
            }
        }

        private async Task ExecuteBackupDatabaseAsync()
        {
            var currentUser = _authStateProvider.CurrentUser;
            if (currentUser == null)
            {
                _notificationService.ShowError("Cannot perform backup. Current user is not authenticated.", "Authentication Error");
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                FileName = $"RuleArchitect_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.sqlite",
                Filter = "SQLite Database Files (*.sqlite)|*.sqlite|All files (*.*)|*.*",
                Title = "Save Database Backup"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                IsLoading = true;
                try
                {
                    bool success = await _databaseService.BackupDatabaseAsync(saveFileDialog.FileName, currentUser.UserId, currentUser.UserName);
                    if (success)
                    {
                        // **SAVE AND UPDATE DISPLAY FROM VIEWMODEL**
                        Settings.Default.LastBackupTime = DateTime.Now;
                        Settings.Default.Save();
                        UpdateLastBackupTime();
                        _notificationService.ShowSuccess($"Database successfully backed up to:\n{saveFileDialog.FileName}", "Backup Complete");
                    }
                    else
                    {
                        _notificationService.ShowError("Failed to create database backup. The database file might be in use or inaccessible. Please check application logs for details.", "Backup Failed", isCritical: true);
                    }
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"An unexpected error occurred during backup: {ex.Message}", "Backup Error", isCritical: true);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                UpdateLastBackupTime();

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

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        RulesheetDistributionSeries.Clear();
                        Brush[] pieColors = new Brush[]
                        {
                            (Brush)Application.Current.FindResource("SecondaryHueLightBrush"),
                            (Brush)Application.Current.FindResource("SecondaryHueMidBrush"),
                            (Brush)Application.Current.FindResource("PrimaryHueDarkBrush"),
                            (Brush)Application.Current.FindResource("SecondaryHueDarkBrush"),
                            Brushes.Orange,
                            Brushes.Green
                        };

                        int colorIndex = 0;
                        foreach (var d in distribution)
                        {
                            RulesheetDistributionSeries.Add(new PieSeries
                            {
                                Title = d.ControlSystem,
                                Values = new ChartValues<int> { d.Count },
                                DataLabels = true,
                                Fill = pieColors[colorIndex % pieColors.Length]
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
                    .GroupBy(l => l.Timestamp.ToLocalTime().Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Date)
                    .ToList();

                var dateRange = Enumerable.Range(0, 7).Select(offset => DateTime.Now.AddDays(-6 + offset).Date);
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
    }
}
