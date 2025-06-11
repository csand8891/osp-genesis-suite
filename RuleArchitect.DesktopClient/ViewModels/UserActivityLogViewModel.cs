// In RuleArchitect.DesktopClient/ViewModels/UserActivityLogViewModel.cs
using HeraldKit.Interfaces;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using RuleArchitect.Abstractions.DTOs;
using RuleArchitect.Abstractions.DTOs.Activity;
using RuleArchitect.Abstractions.Interfaces;
using RuleArchitect.DesktopClient.Commands;
using RuleArchitect.DesktopClient.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RuleArchitect.DesktopClient.ViewModels
{
    public class UserActivityLogViewModel : BaseViewModel
    {
        private readonly IUserActivityLogService _activityLogService;
        private readonly INotificationService _notificationService;
        private readonly IServiceProvider _serviceProvider; // Added for DI

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value, () => ((RelayCommand)LoadLogsCommand).RaiseCanExecuteChanged());
        }

        public ObservableCollection<UserActivityLogDto> ActivityLogs { get; }
        public ObservableCollection<string> QuickFilterTypes { get; }
        public ActivityLogFilterDto CurrentFilter { get; private set; }

        public ICommand LoadLogsCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand ApplyQuickFilterCommand { get; }
        public ICommand GoToTargetCommand { get; } // New command for the link

        public UserActivityLogViewModel(IUserActivityLogService activityLogService, INotificationService notificationService, IServiceProvider serviceProvider)
        {
            _activityLogService = activityLogService;
            _notificationService = notificationService;
            _serviceProvider = serviceProvider; // Store the service provider

            ActivityLogs = new ObservableCollection<UserActivityLogDto>();
            QuickFilterTypes = new ObservableCollection<string>();
            CurrentFilter = new ActivityLogFilterDto();

            LoadLogsCommand = new RelayCommand(async () => await LoadLogsAsync());
            ClearFiltersCommand = new RelayCommand(ClearAndReload);
            ApplyQuickFilterCommand = new RelayCommand(ApplyQuickFilter);
            GoToTargetCommand = new RelayCommand(async (param) => await ExecuteGoToTarget(param), CanGoToTarget);
        }

        private async Task LoadLogsAsync()
        {
            IsLoading = true;
            ActivityLogs.Clear();
            try
            {
                if (!QuickFilterTypes.Any())
                {
                    await LoadQuickFiltersAsync();
                }

                var logs = await _activityLogService.GetActivityLogsAsync(CurrentFilter);

                if (logs != null)
                {
                    foreach (var log in logs)
                    {
                        ActivityLogs.Add(log);
                    }
                }
                _notificationService.ShowInformation($"{ActivityLogs.Count} log entries found.", "Logs Loaded");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to load activity logs: {ex.Message}", "Load Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadQuickFiltersAsync()
        {
            try
            {
                var types = await _activityLogService.GetDistinctActivityTypesAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    QuickFilterTypes.Clear();
                    if (types != null)
                    {
                        foreach (var type in types)
                        {
                            QuickFilterTypes.Add(type);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Could not load quick filters.", "Load Error");
            }
        }

        private void ApplyQuickFilter(object? parameter)
        {
            if (parameter is string activityType)
            {
                CurrentFilter.ActivityTypeFilter = activityType;
                OnPropertyChanged(nameof(CurrentFilter));
                _ = LoadLogsAsync();
            }
        }

        private void ClearAndReload()
        {
            CurrentFilter = new ActivityLogFilterDto();
            OnPropertyChanged(nameof(CurrentFilter));
            _ = LoadLogsAsync();
        }

        private bool CanGoToTarget(object? parameter)
        {
            return parameter is UserActivityLogDto log && log.TargetEntityId.HasValue;
        }

        private async Task ExecuteGoToTarget(object? parameter)
        {
            if (!(parameter is UserActivityLogDto log) || !log.TargetEntityId.HasValue)
            {
                return;
            }

            object? dialogContent = null;

            switch (log.TargetEntityType)
            {
                case "SoftwareOption":
                    var editVm = _serviceProvider.GetRequiredService<EditSoftwareOptionViewModel>();
                    await editVm.LoadSoftwareOptionAsync(log.TargetEntityId.Value);
                    dialogContent = new EditSoftwareOptionView { DataContext = editVm };
                    break;

                default:
                    _notificationService.ShowWarning($"Navigation for target type '{log.TargetEntityType}' is not implemented.", "Navigation Not Supported");
                    break;
            }

            if (dialogContent != null)
            {
                await DialogHost.Show(dialogContent, "RootDialog");
            }
        }
    }
}
