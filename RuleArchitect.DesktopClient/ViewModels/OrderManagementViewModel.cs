// RuleArchitect.DesktopClient/ViewModels/OrderManagementViewModel.cs
using HeraldKit.Interfaces;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using RuleArchitect.Abstractions.DTOs.Order;
using RuleArchitect.Abstractions.Enums;
using RuleArchitect.Abstractions.Interfaces;
using RuleArchitect.DesktopClient.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace RuleArchitect.DesktopClient.ViewModels
{
    public class OrderManagementViewModel : BaseViewModel
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOrderService _orderService;
        private readonly INotificationService _notificationService;

        public ICommand ShowCreateOrderDialogCommand { get; }


        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value, () => ((RelayCommand)LoadOrdersCommand).RaiseCanExecuteChanged());
        }


        public ObservableCollection<OrderDetailDto> Orders { get; }
        public ICollectionView FilteredOrdersView { get; }

        private OrderFilterDto _currentFilter;
        public OrderFilterDto CurrentFilter
        {
            get => _currentFilter;
            set => SetProperty(ref _currentFilter, value);
        }

        public List<OrderStatus> AllOrderStatuses { get; }

        public ICommand LoadOrdersCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand ApplyFiltersCommand { get; }


        public OrderManagementViewModel(IOrderService orderService, INotificationService notificationService, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _orderService = orderService;
            _notificationService = notificationService;

            Orders = new ObservableCollection<OrderDetailDto>();
            FilteredOrdersView = CollectionViewSource.GetDefaultView(Orders);
            FilteredOrdersView.Filter = ApplyFilterPredicate;

            _currentFilter = new OrderFilterDto();
            AllOrderStatuses = Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>().ToList();

            LoadOrdersCommand = new RelayCommand(async () => await LoadOrdersAsync());
            ClearFiltersCommand = new RelayCommand(ClearAndReload);
            ApplyFiltersCommand = new RelayCommand(() => FilteredOrdersView.Refresh());

            ShowCreateOrderDialogCommand = new RelayCommand(async () => await ExecuteShowCreateOrderDialog());
        }

        private async Task LoadOrdersAsync()
        {
            IsLoading = true;
            try
            {
                var orders = await _orderService.GetAllOrdersAsync(new OrderFilterDto()); // Initially load all
                Orders.Clear();
                if (orders != null)
                {
                    foreach (var order in orders)
                    {
                        Orders.Add(order);
                    }
                }
                _notificationService.ShowInformation($"{Orders.Count} orders loaded.", "Orders Loaded");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to load orders: {ex.Message}", "Load Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ClearAndReload()
        {
            CurrentFilter = new OrderFilterDto();
            OnPropertyChanged(nameof(CurrentFilter));
            FilteredOrdersView.Refresh();
        }

        private bool ApplyFilterPredicate(object item)
        {
            if (item is OrderDetailDto order)
            {
                // Status Filter
                if (CurrentFilter.Status.HasValue && order.Status != CurrentFilter.Status.Value)
                {
                    return false;
                }

                // Order Number Filter
                if (!string.IsNullOrWhiteSpace(CurrentFilter.OrderNumber) &&
                    (order.OrderNumber == null || !order.OrderNumber.ToLower().Contains(CurrentFilter.OrderNumber.ToLower())))
                {
                    return false;
                }

                // Customer Name Filter
                if (!string.IsNullOrWhiteSpace(CurrentFilter.CustomerName) &&
                    (order.CustomerName == null || !order.CustomerName.ToLower().Contains(CurrentFilter.CustomerName.ToLower())))
                {
                    return false;
                }

                return true;
            }
            return false;
        }

        private async Task ExecuteShowCreateOrderDialog()
        {
            var dialogViewModel = _serviceProvider.GetRequiredService<CreateOrderFromPdfViewModel>();
            var dialogView = new Views.CreateOrderFromPdfView
            {
                DataContext = dialogViewModel
            };

            // CHANGE "RootDialogHost" to "RootDialog"
            var result = await DialogHost.Show(dialogView, "RootDialog");

            // After the dialog closes, refresh the Kanban board
            if (result is bool wasOrderCreated && wasOrderCreated)
            {
                await LoadOrdersAsync();
            }
        }

    }
}