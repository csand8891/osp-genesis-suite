// File: RuleArchitect.DesktopClient/ViewModels/EditOrderViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using RuleArchitect.Abstractions.DTOs.Order;
using RuleArchitect.Abstractions.Interfaces; // Assuming IOrderService might be needed later
using System;
using System.Threading.Tasks;

namespace RuleArchitect.DesktopClient.ViewModels
{
    public partial class EditOrderViewModel : BaseViewModel
    {
        // private readonly IOrderService _orderService; // If needed for loading/saving

        [ObservableProperty]
        private OrderDetailDto _order;

        // Constructor might take IOrderService, etc. in the future
        public EditOrderViewModel(/* IOrderService orderService */)
        {
            // _orderService = orderService;
            Title = "Edit Order"; // Default title
        }

        public async Task LoadOrderAsync(int orderId)
        {
            IsLoading = true;
            // In a real scenario, you'd fetch this from _orderService
            // For now, this method is a placeholder if called externally.
            // Example:
            // Order = await _orderService.GetOrderByIdAsync(orderId);
            // if (Order != null) Title = $"Edit Order: {Order.OrderNumber}";
            await Task.Delay(100); // Simulate loading
            IsLoading = false;

            // For testing, we might manually create a dummy order if not using service yet
            if (Order == null)
            {
                 Order = new OrderDetailDto { OrderId = orderId, OrderNumber = $"DUMMY-{orderId}", CustomerName = "Dummy Customer" };
                 Title = $"Edit Order: {Order.OrderNumber}";
            }
        }
    }
}
