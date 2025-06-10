// File: RuleArchitect.ApplicationLogic/Services/OrderService.cs
using Microsoft.EntityFrameworkCore;
using RuleArchitect.Abstractions.DTOs.Order;
using RuleArchitect.Abstractions.Interfaces;
using RuleArchitect.Data; // For RuleArchitectContext
using RuleArchitect.Entities;
using HeraldKit.Interfaces; // For INotificationService
using RuleArchitect.Abstractions.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RuleArchitect.ApplicationLogic.Services
{
    /// <summary>
    /// Service implementation for managing orders and their lifecycle.
    /// Handles creation, retrieval, updates, and status changes for orders.
    /// </summary>
    public class OrderService : IOrderService
    {
        private readonly RuleArchitectContext _context;
        private readonly INotificationService _notificationService;
        private readonly ISoftwareOptionService _softwareOptionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderService"/> class.
        /// </summary>
        /// <param name="context">The database context for data access.</param>
        /// <param name="notificationService">The service for sending user notifications.</param>
        /// <param name="softwareOptionService">The service for accessing software option data.</param>
        /// <exception cref="ArgumentNullException">Thrown if any of the injected services are null.</exception>
        public OrderService(
            RuleArchitectContext context,
            INotificationService notificationService,
            ISoftwareOptionService softwareOptionService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _softwareOptionService = softwareOptionService ?? throw new ArgumentNullException(nameof(softwareOptionService));
        }

        /// <summary>
        /// Creates a new order in the system based on the provided data.
        /// Validates input, creates the order and its line items, and saves to the database.
        /// </summary>
        /// <param name="createOrderDto">The data transfer object containing details for the new order.</param>
        /// <param name="createdByUserId">The ID of the user creating the order.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result contains the <see cref="OrderDetailDto"/> of the newly created order.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="createOrderDto"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if required fields like OrderNumber are missing.</exception>
        /// <exception cref="InvalidOperationException">Thrown if an order with the same OrderNumber already exists.</exception>
        /// <exception cref="KeyNotFoundException">Thrown if referenced entities (ControlSystem, MachineModel, SoftwareOption) are not found.</exception>
        public async Task<OrderDetailDto> CreateOrderAsync(CreateOrderDto createOrderDto, int createdByUserId)
        {
            // 1. Validation
            if (createOrderDto == null)
                throw new ArgumentNullException(nameof(createOrderDto));
            if (string.IsNullOrWhiteSpace(createOrderDto.OrderNumber))
                throw new ArgumentException("OrderNumber is required.", nameof(createOrderDto.OrderNumber));

            if (await _context.Orders.AnyAsync(o => o.OrderNumber == createOrderDto.OrderNumber))
            {
                throw new InvalidOperationException($"An order with OrderNumber '{createOrderDto.OrderNumber}' already exists.");
            }

            var controlSystemExists = await _context.ControlSystems.AnyAsync(cs => cs.ControlSystemId == createOrderDto.ControlSystemId);
            if (!controlSystemExists)
                throw new KeyNotFoundException($"ControlSystem with ID {createOrderDto.ControlSystemId} not found.");

            var machineModelExists = await _context.MachineModels.AnyAsync(mm => mm.MachineModelId == createOrderDto.MachineModelId);
            if (!machineModelExists)
                throw new KeyNotFoundException($"MachineModel with ID {createOrderDto.MachineModelId} not found.");

            // 2. Create Order Entity
            var order = new Order
            {
                OrderNumber = createOrderDto.OrderNumber,
                CustomerName = createOrderDto.CustomerName,
                OrderDate = createOrderDto.OrderDate,
                RequiredDate = createOrderDto.RequiredDate,
                Status = OrderStatus.Draft,
                Notes = createOrderDto.Notes,
                ControlSystemId = createOrderDto.ControlSystemId,
                MachineModelId = createOrderDto.MachineModelId,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow,
                LastModifiedByUserId = createdByUserId
            };

            if (createOrderDto.SoftwareOptionIds != null && createOrderDto.SoftwareOptionIds.Any())
            {
                foreach (var soId in createOrderDto.SoftwareOptionIds.Distinct())
                {
                    var softwareOption = await _softwareOptionService.GetSoftwareOptionByIdAsync(soId);
                    if (softwareOption == null)
                    {
                        throw new KeyNotFoundException($"SoftwareOption with ID {soId} not found and cannot be added to order.");
                    }
                    order.OrderItems.Add(new OrderItem { SoftwareOptionId = soId, AddedAt = DateTime.UtcNow });
                }
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            _notificationService.ShowSuccess(
                message: $"Order '{order.OrderNumber}' created successfully by user ID {createdByUserId}.",
                title: "Order Created");

            var createdOrderWithDetails = await _context.Orders
                .Include(o => o.ControlSystem)
                .Include(o => o.MachineModel).ThenInclude(mm => mm.MachineType)
                .Include(o => o.CreatedByUser)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.SoftwareOption)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == order.OrderId);

            return await MapOrderToDetailDtoAsync(createdOrderWithDetails!);
        }

        /// <summary>
        /// Retrieves a specific order by its unique identifier, including all related details.
        /// </summary>
        /// <param name="orderId">The unique ID of the order to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation.
        /// The task result contains the <see cref="OrderDetailDto"/> if found; otherwise, null.</returns>
        public async Task<OrderDetailDto?> GetOrderByIdAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.ControlSystem)
                .Include(o => o.MachineModel).ThenInclude(mm => mm.MachineType)
                .Include(o => o.CreatedByUser)
                .Include(o => o.OrderReviewerUser)
                .Include(o => o.ProductionTechUser)
                .Include(o => o.SoftwareReviewerUser)
                .Include(o => o.LastModifiedByUser)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.SoftwareOption)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return null;
            }
            return await MapOrderToDetailDtoAsync(order);
        }

        /// <summary>
        /// Retrieves a collection of orders based on the specified filter criteria.
        /// </summary>
        /// <param name="filterDto">The data transfer object containing filter parameters.</param>
        /// <returns>A task that represents the asynchronous operation.
        /// The task result contains an enumerable collection of <see cref="OrderDetailDto"/> matching the criteria.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="filterDto"/> is null.</exception>
        public async Task<IEnumerable<OrderDetailDto>> GetAllOrdersAsync(OrderFilterDto filterDto)
        {
            if (filterDto == null)
                throw new ArgumentNullException(nameof(filterDto));

            var query = _context.Orders.AsQueryable();

            if (filterDto.Status.HasValue)
            {
                query = query.Where(o => o.Status == filterDto.Status.Value);
            }
            if (!string.IsNullOrWhiteSpace(filterDto.CustomerName))
            {
                // Ensure CustomerName is not null on the entity before calling ToLower() or Contains()
                query = query.Where(o => o.CustomerName != null && o.CustomerName.ToLower().Contains(filterDto.CustomerName.ToLower()));
            }
            if (!string.IsNullOrWhiteSpace(filterDto.OrderNumber))
            {
                query = query.Where(o => o.OrderNumber.ToLower().Contains(filterDto.OrderNumber.ToLower()));
            }
            if (filterDto.OrderDateFrom.HasValue)
            {
                query = query.Where(o => o.OrderDate >= filterDto.OrderDateFrom.Value.Date); // Use .Date to compare dates only
            }
            if (filterDto.OrderDateTo.HasValue)
            {
                // Add 1 day to OrderDateTo to make the range inclusive of the end date
                var dateTo = filterDto.OrderDateTo.Value.Date.AddDays(1);
                query = query.Where(o => o.OrderDate < dateTo);
            }
            if (filterDto.ControlSystemId.HasValue)
            {
                query = query.Where(o => o.ControlSystemId == filterDto.ControlSystemId.Value);
            }
            if (filterDto.MachineModelId.HasValue)
            {
                query = query.Where(o => o.MachineModelId == filterDto.MachineModelId.Value);
            }

            // Include necessary related data for OrderDetailDto mapping
            var orders = await query
                .Include(o => o.ControlSystem)
                .Include(o => o.MachineModel).ThenInclude(mm => mm.MachineType)
                .Include(o => o.CreatedByUser)
                // Only include OrderItems if they are frequently needed in the list view.
                // For performance, consider if this level of detail is always required for a list.
                // If not, a simpler OrderSummaryDto might be better, and OrderItems loaded on demand.
                .Include(o => o.OrderItems).ThenInclude(oi => oi.SoftwareOption)
                .OrderByDescending(o => o.OrderDate) // Example ordering
                .AsNoTracking()
                .ToListAsync();

            var orderDetailDtos = new List<OrderDetailDto>();
            foreach (var order in orders)
            {
                // MapOrderToDetailDtoAsync is already designed to handle loaded entities.
                var dto = await MapOrderToDetailDtoAsync(order);
                if (dto != null)
                {
                    orderDetailDtos.Add(dto);
                }
            }
            return orderDetailDtos;
        }


        /// <summary>
        /// Puts an order on hold.
        /// Updates the order status to an 'OnHold' state.
        /// </summary>
        /// <param name="orderId">The ID of the order to put on hold.</param>
        /// <param name="userId">The ID of the user putting the order on hold.</param>
        /// <param name="notes">Optional notes explaining the reason for the hold.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result is true if the order was successfully put on hold; otherwise, false.</returns>
        public async Task<bool> PutOrderOnHoldAsync(int orderId, int userId, string? notes)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                _notificationService.ShowError($"Order with ID {orderId} not found.", "Operation Failed");
                return false;
            }

            // Business rule: Check if order can be put on hold
            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
            {
                _notificationService.ShowWarning(
                    $"Order '{order.OrderNumber}' is already {order.Status.ToString().ToLower()} and cannot be put on hold.",
                    "Action Not Allowed");
                return false;
            }

            if (order.Status == OrderStatus.OnHold) // Ensure OrderStatus.OnHold enum member exists
            {
                _notificationService.ShowInformation(
                   $"Order '{order.OrderNumber}' is already on hold.",
                   "Order Status");
                return true; // Already in desired state, no change needed
            }

            order.Status = OrderStatus.OnHold;
            order.LastModifiedAt = DateTime.UtcNow;
            order.LastModifiedByUserId = userId;

            // Append notes if provided
            if (!string.IsNullOrWhiteSpace(notes))
            {
                string holdNote = $"Put on hold by User ID {userId} on {DateTime.UtcNow:yyyy-MM-dd HH:mm}: {notes}";
                order.Notes = string.IsNullOrWhiteSpace(order.Notes)
                    ? holdNote
                    : $"{order.Notes}{Environment.NewLine}{holdNote}";
            }

            try
            {
                await _context.SaveChangesAsync();
                _notificationService.ShowInformation(
                    $"Order '{order.OrderNumber}' has been put on hold by User ID {userId}.",
                    "Order Status Updated");
                return true;
            }
            catch (DbUpdateException ex)
            {
                // TODO: Log the exception (ex) properly using a logging framework
                Console.WriteLine($"Database update error in PutOrderOnHoldAsync: {ex.Message}");
                _notificationService.ShowError(
                    $"Failed to put order '{order.OrderNumber}' on hold. Database error occurred.",
                    "Database Error");
                return false;
            }
        }

        /// <summary>
        /// Updates an existing order with the provided details.
        /// </summary>
        /// <param name="orderId">The unique ID of the order to update.</param>
        /// <param name="updateOrderDto">The data transfer object containing the updated order details.</param>
        /// <param name="modifiedByUserId">The ID of the user performing the update.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result contains the updated <see cref="OrderDetailDto"/> if the update was successful; otherwise, null (e.g., if the order was not found).</returns>
        public Task<OrderDetailDto?> UpdateOrderAsync(int orderId, UpdateOrderDto updateOrderDto, int modifiedByUserId)
        {
            // Placeholder for actual implementation
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes a specific software option (line item) from an order.
        /// </summary>
        /// <param name="orderId">The ID of the order from which to remove the item.</param>
        /// <param name="orderItemId">The ID of the order item (linking to the software option) to remove.</param>
        /// <param name="removedByUserId">The ID of the user performing the removal.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result is true if the item was successfully removed; otherwise, false.</returns>
        public Task<bool> RemoveSoftwareOptionFromOrderAsync(int orderId, int orderItemId, int removedByUserId)
        {
            // Placeholder for actual implementation
            throw new NotImplementedException();
        }

        /// <summary>
        /// Submits an order for production, typically after initial creation and review.
        /// Updates the order status and records the reviewer.
        /// </summary>
        /// <param name="orderId">The ID of the order to submit.</param>
        /// <param name="reviewerUserId">The ID of the user submitting/reviewing the order for production.</param>
        /// <param name="notes">Optional notes for this status change.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result is true if the order was successfully submitted; otherwise, false.</returns>
        public Task<bool> SubmitOrderForProductionAsync(int orderId, int reviewerUserId, string? notes)
        {
            // Placeholder for actual implementation
            throw new NotImplementedException();
        }

        /// <summary>
        /// Marks an order as having started production.
        /// Updates the order status and records the production technician.
        /// </summary>
        /// <param name="orderId">The ID of the order.</param>
        /// <param name="techUserId">The ID of the production technician starting the work.</param>
        /// <param name="notes">Optional notes for this status change.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result is true if the production status was successfully updated; otherwise, false.</returns>
        public Task<bool> StartProductionAsync(int orderId, int techUserId, string? notes)
        {
            // Placeholder for actual implementation
            throw new NotImplementedException();
        }

        /// <summary>
        /// Marks an order's production phase as complete.
        /// Updates the order status and records the production technician.
        /// </summary>
        /// <param name="orderId">The ID of the order.</param>
        /// <param name="techUserId">The ID of the production technician completing the work.</param>
        /// <param name="notes">Optional notes for this status change.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result is true if the production completion status was successfully updated; otherwise, false.</returns>
        public Task<bool> CompleteProductionAsync(int orderId, int techUserId, string? notes)
        {
            // Placeholder for actual implementation
            throw new NotImplementedException();
        }

        /// <summary>
        /// Marks an order as having started the software review phase.
        /// Updates the order status and records the software reviewer.
        /// </summary>
        /// <param name="orderId">The ID of the order.</param>
        /// <param name="reviewerUserId">The ID of the user performing the software review.</param>
        /// <param name="notes">Optional notes for this status change.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result is true if the software review status was successfully updated; otherwise, false.</returns>
        public Task<bool> StartSoftwareReviewAsync(int orderId, int reviewerUserId, string? notes)
        {
            // Placeholder for actual implementation
            throw new NotImplementedException();
        }

        /// <summary>
        /// Rejects an order at some stage in its workflow, setting it to a specified 'rejected' status.
        /// </summary>
        /// <param name="orderId">The ID of the order to reject.</param>
        /// <param name="newStatus">The specific <see cref="OrderStatus"/> to set (e.g., OrderRejected, ReviewRejected). It must be a status that indicates rejection.</param>
        /// <param name="userId">The ID of the user rejecting the order.</param>
        /// <param name="rejectionNotes">Mandatory notes explaining the reason for rejection.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result is true if the order was successfully rejected; otherwise, false.</returns>
        public Task<bool> RejectOrderAsync(int orderId, OrderStatus newStatus, int userId, string rejectionNotes)
        {
            // Placeholder for actual implementation
            throw new NotImplementedException();
        }

        /// <summary>
        /// Cancels an order.
        /// Updates the order status to a 'Cancelled' state.
        /// </summary>
        /// <param name="orderId">The ID of the order to cancel.</param>
        /// <param name="userId">The ID of the user cancelling the order.</param>
        /// <param name="notes">Optional notes explaining the reason for cancellation.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result is true if the order was successfully cancelled; otherwise, false.</returns>
        public Task<bool> CancelOrderAsync(int orderId, int userId, string? notes)
        {
            // Placeholder for actual implementation
            throw new NotImplementedException();
        }

        /// <summary>
        /// Private helper method to map an <see cref="Order"/> entity to an <see cref="OrderDetailDto"/>.
        /// Assumes that necessary related entities (ControlSystem, MachineModel, Users, OrderItems.SoftwareOption)
        /// have been eager-loaded on the input 'order' entity.
        /// </summary>
        /// <param name="order">The <see cref="Order"/> entity to map.</param>
        /// <returns>A task containing the mapped <see cref="OrderDetailDto"/>, or null if the input order is null.</returns>
        private async Task<OrderDetailDto?> MapOrderToDetailDtoAsync(Order order)
        {
            if (order == null) return null;

            return await Task.FromResult(new OrderDetailDto
            {
                OrderId = order.OrderId,
                OrderNumber = order.OrderNumber,
                CustomerName = order.CustomerName,
                OrderDate = order.OrderDate,
                RequiredDate = order.RequiredDate,
                Status = order.Status,
                Notes = order.Notes,
                ControlSystemId = order.ControlSystemId,
                ControlSystemName = order.ControlSystem?.Name,
                MachineModelId = order.MachineModelId,
                MachineModelName = order.MachineModel?.Name,
                MachineTypeName = order.MachineModel?.MachineType?.Name,
                CreatedAt = order.CreatedAt,
                CreatedByUserId = order.CreatedByUserId,
                CreatedByUserName = order.CreatedByUser?.UserName,
                OrderReviewerUserId = order.OrderReviewerUserId,
                OrderReviewerUserName = order.OrderReviewerUser?.UserName,
                OrderReviewedAt = order.OrderReviewedAt,
                OrderReviewNotes = order.OrderReviewNotes,
                ProductionTechUserId = order.ProductionTechUserId,
                ProductionTechUserName = order.ProductionTechUser?.UserName,
                ProductionCompletedAt = order.ProductionCompletedAt,
                ProductionNotes = order.ProductionNotes,
                SoftwareReviewerUserId = order.SoftwareReviewerUserId,
                SoftwareReviewerUserName = order.SoftwareReviewerUser?.UserName,
                SoftwareReviewedAt = order.SoftwareReviewedAt,
                SoftwareReviewNotes = order.SoftwareReviewNotes,
                LastModifiedAt = order.LastModifiedAt,
                LastModifiedByUserId = order.LastModifiedByUserId,
                LastModifiedByUserName = order.LastModifiedByUser?.UserName,
                OrderItems = order.OrderItems?.Select(oi => new OrderItemDto
                {
                    OrderItemId = oi.OrderItemId,
                    SoftwareOptionId = oi.SoftwareOptionId,
                    SoftwareOptionName = oi.SoftwareOption?.PrimaryName,
                    SoftwareOptionNumberDisplay = oi.SoftwareOption?.PrimaryOptionNumberDisplay,
                    AddedAt = oi.AddedAt
                }).ToList() ?? new List<OrderItemDto>()
            });
        }
    }
}
