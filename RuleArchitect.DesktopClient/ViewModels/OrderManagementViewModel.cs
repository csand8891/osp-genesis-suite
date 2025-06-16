// File: RuleArchitect.DesktopClient/ViewModels/OrderManagementViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using RuleArchitect.Abstractions.DTOs.Order;
using RuleArchitect.Abstractions.DTOs.User;
using RuleArchitect.Abstractions.Enums;
using RuleArchitect.Abstractions.Interfaces;
using RuleArchitect.DesktopClient.Interfaces;
using RuleArchitect.DesktopClient.Services;


namespace RuleArchitect.DesktopClient.ViewModels
{
    public partial class OrderManagementViewModel : BaseViewModel
    {
        private readonly IOrderService _orderService;
        private readonly IAuthenticationStateProvider _authenticationStateProvider;
        private readonly INotificationService _desktopNotificationService; // To distinguish from HeraldKit INotificationService
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ISnackbarMessageQueue _snackbarMessageQueue;

        [ObservableProperty]
        private ObservableCollection<OrderDetailDto> _orders;

        [ObservableProperty]
        private ICollectionView _filteredOrdersView;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SubmitOrderForProductionCommand))]
        [NotifyCanExecuteChangedFor(nameof(StartProductionCommand))]
        [NotifyCanExecuteChangedFor(nameof(CompleteProductionCommand))]
        [NotifyCanExecuteChangedFor(nameof(StartSoftwareReviewCommand))]
        [NotifyCanExecuteChangedFor(nameof(RejectOrderCommand))]
        [NotifyCanExecuteChangedFor(nameof(CancelOrderCommand))]
        [NotifyCanExecuteChangedFor(nameof(PutOrderOnHoldCommand))]
        private OrderDetailDto? _selectedOrder;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private UserDto? _currentUser;

        private string? _searchText;
        public string? SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                FilteredOrdersView?.Refresh();
            }
        }

        private OrderStatus? _selectedOrderStatusFilter;
        public OrderStatus? SelectedOrderStatusFilter
        {
            get => _selectedOrderStatusFilter;
            set
            {
                SetProperty(ref _selectedOrderStatusFilter, value);
                FilteredOrdersView?.Refresh();
            }
        }

        public ObservableCollection<OrderStatus> AllOrderStatuses { get; }

        public IAsyncRelayCommand LoadOrdersCommand { get; }
        public IAsyncRelayCommand SubmitOrderForProductionCommand { get; }
        public IAsyncRelayCommand StartProductionCommand { get; }
        public IAsyncRelayCommand CompleteProductionCommand { get; }
        public IAsyncRelayCommand StartSoftwareReviewCommand { get; }
        public IAsyncRelayCommand RejectOrderCommand { get; }
        public IAsyncRelayCommand CancelOrderCommand { get; }
        public IAsyncRelayCommand PutOrderOnHoldCommand { get; }


        public OrderManagementViewModel(
            IOrderService orderService,
            IAuthenticationStateProvider authenticationStateProvider,
            INotificationService desktopNotificationService,
            IServiceScopeFactory serviceScopeFactory,
            ISnackbarMessageQueue snackbarMessageQueue)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _authenticationStateProvider = authenticationStateProvider ?? throw new ArgumentNullException(nameof(authenticationStateProvider));
            _desktopNotificationService = desktopNotificationService ?? throw new ArgumentNullException(nameof(desktopNotificationService));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _snackbarMessageQueue = snackbarMessageQueue ?? throw new ArgumentNullException(nameof(snackbarMessageQueue));

            Title = "Order Management";
            Orders = new ObservableCollection<OrderDetailDto>();
            FilteredOrdersView = CollectionViewSource.GetDefaultView(Orders);
            FilteredOrdersView.Filter = ApplyFilterPredicate;

            AllOrderStatuses = new ObservableCollection<OrderStatus>((OrderStatus[])Enum.GetValues(typeof(OrderStatus)));

            LoadOrdersCommand = new AsyncRelayCommand(ExecuteLoadOrdersAsync, () => !IsLoading);

            // Workflow Commands
            SubmitOrderForProductionCommand = new AsyncRelayCommand(ExecuteSubmitOrderForProductionAsync, CanExecuteWorkflowAction);
            StartProductionCommand = new AsyncRelayCommand(ExecuteStartProductionAsync, CanExecuteWorkflowAction);
            CompleteProductionCommand = new AsyncRelayCommand(ExecuteCompleteProductionAsync, CanExecuteWorkflowAction);
            StartSoftwareReviewCommand = new AsyncRelayCommand(ExecuteStartSoftwareReviewAsync, CanExecuteWorkflowAction);
            RejectOrderCommand = new AsyncRelayCommand(ExecuteRejectOrderAsync, CanExecuteWorkflowAction);
            CancelOrderCommand = new AsyncRelayCommand(ExecuteCancelOrderAsync, CanExecuteWorkflowAction);
            PutOrderOnHoldCommand = new AsyncRelayCommand(ExecutePutOrderOnHoldAsync, CanExecuteWorkflowAction);

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            CurrentUser = await _authenticationStateProvider.GetCurrentUserAsync();
            await ExecuteLoadOrdersAsync();
        }

        private bool ApplyFilterPredicate(object item)
        {
            if (item is not OrderDetailDto order)
                return false;

            bool statusFilterMatches = !SelectedOrderStatusFilter.HasValue || order.Status == SelectedOrderStatusFilter.Value;

            bool textFilterMatches = true;
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                textFilterMatches = order.OrderNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                    (order.CustomerName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                    (order.ControlSystemName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                    (order.MachineModelName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false);
            }

            return statusFilterMatches && textFilterMatches;
        }

        private async Task ExecuteLoadOrdersAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            LoadOrdersCommand.NotifyCanExecuteChanged(); // Reflect IsLoading state

            try
            {
                // In a real app, use a proper filter DTO, possibly with paging
                var filter = new OrderFilterDto { /* define default filters if any */ };
                var ordersList = await _orderService.GetAllOrdersAsync(filter);

                Orders.Clear();
                foreach (var order in ordersList.OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.OrderId))
                {
                    Orders.Add(order);
                }
                _snackbarMessageQueue.Enqueue($"{Orders.Count} orders loaded.");
            }
            catch (Exception ex)
            {
                _desktopNotificationService.ShowError($"Error loading orders: {ex.Message}", "Load Failed");
                // Consider logging ex to a more persistent log
            }
            finally
            {
                IsLoading = false;
                LoadOrdersCommand.NotifyCanExecuteChanged();
            }
        }

        private bool CanExecuteWorkflowAction()
        {
            return SelectedOrder != null && CurrentUser != null && !IsLoading;
        }

        // Example Workflow Command Implementation (SubmitOrderForProduction)
        private async Task ExecuteSubmitOrderForProductionAsync()
        {
            if (!CanExecuteWorkflowAction() || SelectedOrder == null || CurrentUser == null) return;
            if (SelectedOrder.Status != OrderStatus.Draft)
            {
                _desktopNotificationService.ShowWarning("Order must be in Draft status to submit for production.", "Action Not Allowed");
                return;
            }

            // In a real app, prompt for notes
            string? notes = "Submitted via Desktop Client";

            IsLoading = true;
            try
            {
                var success = await _orderService.SubmitOrderForProductionAsync(SelectedOrder.OrderId, CurrentUser.UserId, notes);
                if (success)
                {
                    _snackbarMessageQueue.Enqueue($"Order '{SelectedOrder.OrderNumber}' submitted for production.");
                    await ExecuteLoadOrdersAsync(); // Refresh list
                }
                // IOrderService implementation should show error/warning notifications via HeraldKit
            }
            catch (Exception ex)
            {
                _desktopNotificationService.ShowError($"Error submitting order: {ex.Message}", "Operation Failed");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Placeholder implementations for other workflow commands
        private async Task ExecuteStartProductionAsync()
        {
            if (!CanExecuteWorkflowAction() || SelectedOrder == null || CurrentUser == null) return;
             if (SelectedOrder.Status != OrderStatus.ReadyForProduction)
            {
                _desktopNotificationService.ShowWarning("Order must be in 'Ready For Production' status to start production.", "Action Not Allowed");
                return;
            }
            // TODO: Prompt for notes
            string? notes = "Production started via Desktop Client";
            IsLoading = true;
            try
            {
                var success = await _orderService.StartProductionAsync(SelectedOrder.OrderId, CurrentUser.UserId, notes);
                if (success) { _snackbarMessageQueue.Enqueue("Production started for " + SelectedOrder.OrderNumber); await ExecuteLoadOrdersAsync(); }
            }
            catch (Exception ex) { _desktopNotificationService.ShowError(ex.Message, "Start Production Failed"); }
            finally { IsLoading = false; }
        }

        private async Task ExecuteCompleteProductionAsync()
        {
            if (!CanExecuteWorkflowAction() || SelectedOrder == null || CurrentUser == null) return;
            if (SelectedOrder.Status != OrderStatus.ProductionInProgress)
            {
                _desktopNotificationService.ShowWarning("Order must be 'In Production' to complete production.", "Action Not Allowed");
                return;
            }
            string? notes = "Production completed via Desktop Client";
            IsLoading = true;
            try
            {
                var success = await _orderService.CompleteProductionAsync(SelectedOrder.OrderId, CurrentUser.UserId, notes);
                if (success) { _snackbarMessageQueue.Enqueue("Production completed for " + SelectedOrder.OrderNumber); await ExecuteLoadOrdersAsync(); }
            }
            catch (Exception ex) { _desktopNotificationService.ShowError(ex.Message, "Complete Production Failed"); }
            finally { IsLoading = false; }
        }
         private async Task ExecuteStartSoftwareReviewAsync()
        {
            if (!CanExecuteWorkflowAction() || SelectedOrder == null || CurrentUser == null) return;
            if (SelectedOrder.Status != OrderStatus.ReadyForSoftwareReview)
            {
                _desktopNotificationService.ShowWarning("Order must be 'Ready For Software Review' to start review.", "Action Not Allowed");
                return;
            }
            string? notes = "Software review started via Desktop Client";
            IsLoading = true;
            try
            {
                var success = await _orderService.StartSoftwareReviewAsync(SelectedOrder.OrderId, CurrentUser.UserId, notes);
                if (success) { _snackbarMessageQueue.Enqueue("Software review started for " + SelectedOrder.OrderNumber); await ExecuteLoadOrdersAsync(); }
            }
            catch (Exception ex) { _desktopNotificationService.ShowError(ex.Message, "Start Software Review Failed"); }
            finally { IsLoading = false; }
        }

        private async Task ExecuteRejectOrderAsync()
        {
            if (!CanExecuteWorkflowAction() || SelectedOrder == null || CurrentUser == null) return;
            // TODO: Prompt for rejection notes (mandatory) and confirm which 'Rejected' status if applicable
            // For now, uses generic OrderStatus.Rejected
            string rejectionNotes = "Order rejected via Desktop Client.";
             var validPreviousStates = new[] {
                OrderStatus.ReadyForProduction, OrderStatus.ProductionInProgress,
                OrderStatus.ReadyForSoftwareReview, OrderStatus.SoftwareReviewInProgress
            };
            if (!validPreviousStates.Contains(SelectedOrder.Status))
            {
                 _desktopNotificationService.ShowWarning($"Order cannot be rejected from '{SelectedOrder.Status}' status.", "Action Not Allowed");
                return;
            }
            IsLoading = true;
            try
            {
                var success = await _orderService.RejectOrderAsync(SelectedOrder.OrderId, OrderStatus.Rejected, CurrentUser.UserId, rejectionNotes);
                if (success) { _snackbarMessageQueue.Enqueue("Order rejected: " + SelectedOrder.OrderNumber); await ExecuteLoadOrdersAsync(); }
            }
            catch (Exception ex) { _desktopNotificationService.ShowError(ex.Message, "Reject Order Failed"); }
            finally { IsLoading = false; }
        }

        private async Task ExecuteCancelOrderAsync()
        {
            if (!CanExecuteWorkflowAction() || SelectedOrder == null || CurrentUser == null) return;
            if (SelectedOrder.Status == OrderStatus.Completed || SelectedOrder.Status == OrderStatus.Cancelled)
            {
                 _desktopNotificationService.ShowWarning($"Order is '{SelectedOrder.Status}' and cannot be cancelled.", "Action Not Allowed");
                return;
            }
            string? notes = "Order cancelled via Desktop Client";
            IsLoading = true;
            try
            {
                var success = await _orderService.CancelOrderAsync(SelectedOrder.OrderId, CurrentUser.UserId, notes);
                if (success) { _snackbarMessageQueue.Enqueue("Order cancelled: " + SelectedOrder.OrderNumber); await ExecuteLoadOrdersAsync(); }
            }
            catch (Exception ex) { _desktopNotificationService.ShowError(ex.Message, "Cancel Order Failed"); }
            finally { IsLoading = false; }
        }

        private async Task ExecutePutOrderOnHoldAsync()
        {
            if (!CanExecuteWorkflowAction() || SelectedOrder == null || CurrentUser == null) return;
            if (SelectedOrder.Status == OrderStatus.Completed || SelectedOrder.Status == OrderStatus.Cancelled)
            {
                 _desktopNotificationService.ShowWarning($"Order is '{SelectedOrder.Status}' and cannot be put on hold.", "Action Not Allowed");
                return;
            }
            string? notes = "Order put on hold via Desktop Client";
            IsLoading = true;
            try
            {
                var success = await _orderService.PutOrderOnHoldAsync(SelectedOrder.OrderId, CurrentUser.UserId, notes);
                if (success) { _snackbarMessageQueue.Enqueue("Order on hold: " + SelectedOrder.OrderNumber); await ExecuteLoadOrdersAsync(); }
            }
            catch (Exception ex) { _desktopNotificationService.ShowError(ex.Message, "Put on Hold Failed"); }
            finally { IsLoading = false; }
        }
    }
}
