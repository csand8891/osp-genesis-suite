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
        private readonly IUserActivityLogService _activityLogService;
        private readonly IUserService _userService;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderService"/> class.
        /// </summary>
        /// <param name="context">The database context for data access.</param>
        /// <param name="notificationService">The service for sending user notifications.</param>
        /// <param name="softwareOptionService">The service for accessing software option data.</param>
        /// <param name="activityLogService">Service for logging user activities.</param>
        /// <param name="userService">Service for user-related operations.</param>
        /// <exception cref="ArgumentNullException">Thrown if any of the injected services are null.</exception>
        public OrderService(
            RuleArchitectContext context,
            INotificationService notificationService,
            ISoftwareOptionService softwareOptionService,
            IUserActivityLogService activityLogService,
            IUserService userService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _softwareOptionService = softwareOptionService ?? throw new ArgumentNullException(nameof(softwareOptionService));
            _activityLogService = activityLogService ?? throw new ArgumentNullException(nameof(activityLogService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
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
        public async Task<OrderDetailDto?> UpdateOrderAsync(int orderId, UpdateOrderDto updateOrderDto, int modifiedByUserId)
        {
            if (updateOrderDto == null)
            {
                _notificationService.ShowError("Update data is null.", "Validation Error");
                return null;
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.CreatedByUser) // For mapping
                .Include(o => o.OrderReviewerUser) // For mapping
                .Include(o => o.ProductionTechUser) // For mapping
                .Include(o => o.SoftwareReviewerUser) // For mapping
                .Include(o => o.ControlSystem) // For mapping
                .Include(o => o.MachineModel).ThenInclude(mm => mm.MachineType) // For mapping
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                _notificationService.ShowError($"Order with ID {orderId} not found.", "Operation Failed");
                return null;
            }

            // Rule: Cannot update a completed or cancelled order
            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
            {
                _notificationService.ShowWarning(
                    $"Order '{order.OrderNumber}' is {order.Status.ToString().ToLower()} and cannot be updated.",
                    "Action Not Allowed");
                return await MapOrderToDetailDtoAsync(order); // Return current state
            }

            // Update basic fields
            order.CustomerName = updateOrderDto.CustomerName;
            order.RequiredDate = updateOrderDto.RequiredDate;
            order.Notes = updateOrderDto.Notes; // Overwrites existing notes as per typical update behavior

            // Update ControlSystemId if changed
            if (order.ControlSystemId != updateOrderDto.ControlSystemId)
            {
                var controlSystemExists = await _context.ControlSystems.AnyAsync(cs => cs.ControlSystemId == updateOrderDto.ControlSystemId);
                if (!controlSystemExists)
                {
                    _notificationService.ShowError($"ControlSystem with ID {updateOrderDto.ControlSystemId} not found.", "Validation Error");
                    return null; // Or return current state if preferred
                }
                order.ControlSystemId = updateOrderDto.ControlSystemId;
            }

            // Update MachineModelId if changed
            if (order.MachineModelId != updateOrderDto.MachineModelId)
            {
                var machineModelExists = await _context.MachineModels.AnyAsync(mm => mm.MachineModelId == updateOrderDto.MachineModelId);
                if (!machineModelExists)
                {
                    _notificationService.ShowError($"MachineModel with ID {updateOrderDto.MachineModelId} not found.", "Validation Error");
                    return null; // Or return current state
                }
                order.MachineModelId = updateOrderDto.MachineModelId;
            }

            // Manage OrderItems
            var currentSoftwareOptionIds = order.OrderItems.Select(oi => oi.SoftwareOptionId).ToList();
            var dtoSoftwareOptionIds = updateOrderDto.SoftwareOptionIds?.Distinct().ToList() ?? new List<int>();

            // Items to remove
            var itemsToRemove = order.OrderItems
                .Where(oi => !dtoSoftwareOptionIds.Contains(oi.SoftwareOptionId))
                .ToList();
            if (itemsToRemove.Any())
            {
                _context.OrderItems.RemoveRange(itemsToRemove);
            }

            // Items to add
            var softwareOptionIdsToAdd = dtoSoftwareOptionIds
                .Where(id => !currentSoftwareOptionIds.Contains(id))
                .ToList();

            foreach (var soId in softwareOptionIdsToAdd)
            {
                var softwareOption = await _softwareOptionService.GetSoftwareOptionByIdAsync(soId);
                if (softwareOption == null)
                {
                    _notificationService.ShowError($"SoftwareOption with ID {soId} not found. Order update partially failed.", "Validation Error");
                    // Decide if to proceed or halt. For now, we skip adding this item.
                    // Consider transactional behavior or more robust error handling here.
                    continue;
                }
                order.OrderItems.Add(new OrderItem { SoftwareOptionId = soId, OrderId = order.OrderId, AddedAt = DateTime.UtcNow });
            }

            order.LastModifiedAt = DateTime.UtcNow;
            order.LastModifiedByUserId = modifiedByUserId;

            var user = await _userService.GetUserByIdAsync(modifiedByUserId); // Fetch user for logging
            await _activityLogService.LogActivityAsync(
                userId: modifiedByUserId,
                userName: user?.UserName ?? $"User ID {modifiedByUserId}", // Fallback username
                action: "UpdateOrder",
                details: $"Order '{order.OrderNumber}' (ID: {order.OrderId}) was updated.",
                targetEntityType: "Order",
                targetEntityId: order.OrderId);

            try
            {
                await _context.SaveChangesAsync();
                _notificationService.ShowSuccess($"Order '{order.OrderNumber}' updated successfully.", "Order Updated");

                // Re-fetch to get all includes correctly for mapping, especially if new items were added without full SO details
                 var updatedOrder = await _context.Orders
                    .Include(o => o.ControlSystem)
                    .Include(o => o.MachineModel).ThenInclude(mm => mm.MachineType)
                    .Include(o => o.CreatedByUser)
                    .Include(o => o.OrderReviewerUser)
                    .Include(o => o.ProductionTechUser)
                    .Include(o => o.SoftwareReviewerUser)
                    .Include(o => o.LastModifiedByUser) // Make sure this is loaded
                    .Include(o => o.OrderItems).ThenInclude(oi => oi.SoftwareOption)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                return await MapOrderToDetailDtoAsync(updatedOrder!);
            }
            catch (DbUpdateException ex)
            {
                // TODO: Log ex
                _notificationService.ShowError($"Failed to update order '{order.OrderNumber}'. Database error: {ex.Message}", "Database Error");
                return null;
            }
        }

        /// <summary>
        /// Removes a specific software option (line item) from an order.
        /// </summary>
        /// <param name="orderId">The ID of the order from which to remove the item.</param>
        /// <param name="orderItemId">The ID of the order item (linking to the software option) to remove.</param>
        /// <param name="removedByUserId">The ID of the user performing the removal.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result is true if the item was successfully removed; otherwise, false.</returns>
        public async Task<bool> RemoveSoftwareOptionFromOrderAsync(int orderId, int orderItemId, int removedByUserId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                _notificationService.ShowError($"Order with ID {orderId} not found.", "Operation Failed");
                return false;
            }

            // Business rule: Check if order status allows item removal
            // Example: Cannot remove items if order is completed, cancelled, or too far in production.
            if (order.Status == OrderStatus.Completed ||
                order.Status == OrderStatus.Cancelled ||
                order.Status == OrderStatus.ReadyForSoftwareReview || // Example: Too late to remove
                order.Status == OrderStatus.SoftwareReviewInProgress) // Example: Too late to remove
            {
                _notificationService.ShowWarning(
                    $"Cannot remove items from order '{order.OrderNumber}' because it is {order.Status.ToString().ToLower()}.",
                    "Action Not Allowed");
                return false;
            }

            var orderItemToRemove = order.OrderItems.FirstOrDefault(oi => oi.OrderItemId == orderItemId);

            if (orderItemToRemove == null)
            {
                _notificationService.ShowError($"Software option (item ID: {orderItemId}) not found in order '{order.OrderNumber}'.", "Operation Failed");
                return false;
            }

            _context.OrderItems.Remove(orderItemToRemove); // EF Core will track this removal
            // order.OrderItems.Remove(orderItemToRemove); // This also works if the relationship is correctly configured for cascading deletes or if you manually save. Explicitly removing from context is safer.

            order.LastModifiedAt = DateTime.UtcNow;
            order.LastModifiedByUserId = removedByUserId;

            // For logging details, it's good to know what was removed.
            // This requires SoftwareOption to be loaded on orderItemToRemove, or fetching it.
            // Assuming orderItemToRemove.SoftwareOption might not be loaded, we log IDs.
            // If SoftwareOption was guaranteed to be loaded via .Include() on the initial order query, we could use its name.
            var user = await _userService.GetUserByIdAsync(removedByUserId);
            await _activityLogService.LogActivityAsync(
                userId: removedByUserId,
                userName: user?.UserName ?? $"User ID {removedByUserId}",
                action: "RemoveSoftwareOptionFromOrder",
                details: $"Removed software option (Item ID: {orderItemId}, SO ID: {orderItemToRemove.SoftwareOptionId}) from order '{order.OrderNumber}' (ID: {order.OrderId}).",
                targetEntityType: "Order",
                targetEntityId: order.OrderId);

            try
            {
                await _context.SaveChangesAsync();
                _notificationService.ShowSuccess($"Software option removed successfully from order '{order.OrderNumber}'.", "Item Removed");
                return true;
            }
            catch (DbUpdateException ex)
            {
                // TODO: Log ex
                _notificationService.ShowError($"Failed to remove software option from order '{order.OrderNumber}'. Database error: {ex.Message}", "Database Error");
                return false;
            }
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
        public async Task<bool> SubmitOrderForProductionAsync(int orderId, int reviewerUserId, string? notes)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                _notificationService.ShowError($"Order with ID {orderId} not found.", "Operation Failed");
                return false;
            }

            // Business rule: Order can only be submitted for production if it's in Draft status.
            if (order.Status != OrderStatus.Draft)
            {
                _notificationService.ShowWarning(
                    $"Order '{order.OrderNumber}' is currently in '{order.Status}' status and cannot be submitted for production. Expected status: '{OrderStatus.Draft}'.",
                    "Action Not Allowed");
                return false;
            }

            order.Status = OrderStatus.ReadyForProduction;
            order.OrderReviewerUserId = reviewerUserId;
            order.OrderReviewedAt = DateTime.UtcNow;
            order.OrderReviewNotes = notes; // Overwrites previous review notes if any
            order.LastModifiedAt = DateTime.UtcNow;
            order.LastModifiedByUserId = reviewerUserId;

            var reviewerUser = await _userService.GetUserByIdAsync(reviewerUserId);
            await _activityLogService.LogActivityAsync(
                userId: reviewerUserId,
                userName: reviewerUser?.UserName ?? $"User ID {reviewerUserId}",
                action: "SubmitOrderForProduction",
                details: $"Order '{order.OrderNumber}' (ID: {order.OrderId}) submitted for production. Review notes: {notes ?? "N/A"}",
                targetEntityType: "Order",
                targetEntityId: order.OrderId);

            try
            {
                await _context.SaveChangesAsync();
                _notificationService.ShowSuccess($"Order '{order.OrderNumber}' has been submitted for production.", "Order Status Updated");
                return true;
            }
            catch (DbUpdateException ex)
            {
                // TODO: Log ex
                _notificationService.ShowError($"Failed to submit order '{order.OrderNumber}' for production. Database error: {ex.Message}", "Database Error");
                return false;
            }
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
        public async Task<bool> StartProductionAsync(int orderId, int techUserId, string? notes)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                _notificationService.ShowError($"Order with ID {orderId} not found.", "Operation Failed");
                return false;
            }

            // Business rule: Production can only start if the order is ReadyForProduction.
            if (order.Status != OrderStatus.ReadyForProduction)
            {
                _notificationService.ShowWarning(
                    $"Order '{order.OrderNumber}' is currently in '{order.Status}' status and production cannot be started. Expected status: '{OrderStatus.ReadyForProduction}'.",
                    "Action Not Allowed");
                return false;
            }

            order.Status = OrderStatus.ProductionInProgress;
            order.ProductionTechUserId = techUserId; // Assigns the tech starting production

            if (!string.IsNullOrWhiteSpace(notes))
            {
                string productionStartNote = $"Production started by User ID {techUserId} on {DateTime.UtcNow:yyyy-MM-dd HH:mm}: {notes}";
                order.ProductionNotes = string.IsNullOrWhiteSpace(order.ProductionNotes)
                    ? productionStartNote
                    : $"{order.ProductionNotes}{Environment.NewLine}{productionStartNote}";
            }

            order.LastModifiedAt = DateTime.UtcNow;
            order.LastModifiedByUserId = techUserId;

            var techUser = await _userService.GetUserByIdAsync(techUserId);
            await _activityLogService.LogActivityAsync(
                userId: techUserId,
                userName: techUser?.UserName ?? $"User ID {techUserId}",
                action: "StartProduction",
                details: $"Production started for order '{order.OrderNumber}' (ID: {order.OrderId}). Notes: {notes ?? "N/A"}",
                targetEntityType: "Order",
                targetEntityId: order.OrderId);

            try
            {
                await _context.SaveChangesAsync();
                _notificationService.ShowSuccess($"Production has started for order '{order.OrderNumber}'.", "Order Status Updated");
                return true;
            }
            catch (DbUpdateException ex)
            {
                // TODO: Log ex
                _notificationService.ShowError($"Failed to start production for order '{order.OrderNumber}'. Database error: {ex.Message}", "Database Error");
                return false;
            }
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
        public async Task<bool> CompleteProductionAsync(int orderId, int techUserId, string? notes)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                _notificationService.ShowError($"Order with ID {orderId} not found.", "Operation Failed");
                return false;
            }

            // Business rule: Production can only be completed if it's in ProductionInProgress.
            if (order.Status != OrderStatus.ProductionInProgress)
            {
                _notificationService.ShowWarning(
                    $"Order '{order.OrderNumber}' is currently in '{order.Status}' status and production cannot be marked as complete. Expected status: '{OrderStatus.ProductionInProgress}'.",
                    "Action Not Allowed");
                return false;
            }

            order.Status = OrderStatus.ReadyForSoftwareReview;
            order.ProductionTechUserId = techUserId; // Confirms or updates the tech user ID for this phase
            order.ProductionCompletedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(notes))
            {
                string productionCompleteNote = $"Production completed by User ID {techUserId} on {DateTime.UtcNow:yyyy-MM-dd HH:mm}: {notes}";
                order.ProductionNotes = string.IsNullOrWhiteSpace(order.ProductionNotes)
                    ? productionCompleteNote
                    : $"{order.ProductionNotes}{Environment.NewLine}{productionCompleteNote}";
            }

            order.LastModifiedAt = DateTime.UtcNow;
            order.LastModifiedByUserId = techUserId;

            var techUser = await _userService.GetUserByIdAsync(techUserId);
            await _activityLogService.LogActivityAsync(
                userId: techUserId,
                userName: techUser?.UserName ?? $"User ID {techUserId}",
                action: "CompleteProduction",
                details: $"Production completed for order '{order.OrderNumber}' (ID: {order.OrderId}). Notes: {notes ?? "N/A"}",
                targetEntityType: "Order",
                targetEntityId: order.OrderId);

            try
            {
                await _context.SaveChangesAsync();
                _notificationService.ShowSuccess($"Production has been completed for order '{order.OrderNumber}'. Order is now Ready for Software Review.", "Order Status Updated");
                return true;
            }
            catch (DbUpdateException ex)
            {
                // TODO: Log ex
                _notificationService.ShowError($"Failed to complete production for order '{order.OrderNumber}'. Database error: {ex.Message}", "Database Error");
                return false;
            }
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
        public async Task<bool> StartSoftwareReviewAsync(int orderId, int reviewerUserId, string? notes)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                _notificationService.ShowError($"Order with ID {orderId} not found.", "Operation Failed");
                return false;
            }

            // Business rule: Software review can only start if the order is ReadyForSoftwareReview.
            if (order.Status != OrderStatus.ReadyForSoftwareReview)
            {
                _notificationService.ShowWarning(
                    $"Order '{order.OrderNumber}' is currently in '{order.Status}' status and software review cannot be started. Expected status: '{OrderStatus.ReadyForSoftwareReview}'.",
                    "Action Not Allowed");
                return false;
            }

            order.Status = OrderStatus.SoftwareReviewInProgress;
            order.SoftwareReviewerUserId = reviewerUserId;
            // SoftwareReviewedAt will be set when review is completed or a final decision is made.

            if (!string.IsNullOrWhiteSpace(notes))
            {
                string reviewStartNote = $"Software review started by User ID {reviewerUserId} on {DateTime.UtcNow:yyyy-MM-dd HH:mm}: {notes}";
                order.SoftwareReviewNotes = string.IsNullOrWhiteSpace(order.SoftwareReviewNotes)
                    ? reviewStartNote
                    : $"{order.SoftwareReviewNotes}{Environment.NewLine}{reviewStartNote}";
            }

            order.LastModifiedAt = DateTime.UtcNow;
            order.LastModifiedByUserId = reviewerUserId;

            var reviewerUser = await _userService.GetUserByIdAsync(reviewerUserId);
            await _activityLogService.LogActivityAsync(
                userId: reviewerUserId,
                userName: reviewerUser?.UserName ?? $"User ID {reviewerUserId}",
                action: "StartSoftwareReview",
                details: $"Software review started for order '{order.OrderNumber}' (ID: {order.OrderId}). Notes: {notes ?? "N/A"}",
                targetEntityType: "Order",
                targetEntityId: order.OrderId);

            try
            {
                await _context.SaveChangesAsync();
                _notificationService.ShowSuccess($"Software review has started for order '{order.OrderNumber}'.", "Order Status Updated");
                return true;
            }
            catch (DbUpdateException ex)
            {
                // TODO: Log ex
                _notificationService.ShowError($"Failed to start software review for order '{order.OrderNumber}'. Database error: {ex.Message}", "Database Error");
                return false;
            }
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
        public async Task<bool> RejectOrderAsync(int orderId, OrderStatus newStatus, int userId, string rejectionNotes)
        {
            if (newStatus != OrderStatus.Rejected)
            {
                _notificationService.ShowError("Invalid status for rejection. Only OrderStatus.Rejected is allowed.", "Validation Error");
                return false;
            }
            if (string.IsNullOrWhiteSpace(rejectionNotes))
            {
                _notificationService.ShowWarning("Rejection notes are mandatory when rejecting an order.", "Validation Error");
                return false; // Or handle as a bad request if this were an API controller
            }

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                _notificationService.ShowError($"Order with ID {orderId} not found.", "Operation Failed");
                return false;
            }

            // Business rule: Define states from which an order can be rejected.
            var allowedPreviousStates = new[] {
                OrderStatus.ReadyForProduction,
                OrderStatus.ProductionInProgress,
                OrderStatus.ReadyForSoftwareReview,
                OrderStatus.SoftwareReviewInProgress
            };

            if (!allowedPreviousStates.Contains(order.Status))
            {
                _notificationService.ShowWarning(
                    $"Order '{order.OrderNumber}' is currently in '{order.Status}' status and cannot be rejected from this state.",
                    "Action Not Allowed");
                return false;
            }
            if (order.Status == OrderStatus.Rejected)
            {
                 _notificationService.ShowInformation($"Order '{order.OrderNumber}' is already rejected.", "Order Status");
                return true; // Already in the desired state.
            }


            order.Status = newStatus; // Should be OrderStatus.Rejected

            string rejectionEntry = $"Order rejected by User ID {userId} on {DateTime.UtcNow:yyyy-MM-dd HH:mm}: {rejectionNotes}";
            order.Notes = string.IsNullOrWhiteSpace(order.Notes)
                ? rejectionEntry
                : $"{order.Notes}{Environment.NewLine}{rejectionEntry}";

            // Potentially clear or update specific review/production fields if rejection means re-doing those steps
            // For example, if rejected during ReadyForProduction, clear OrderReviewerUserId, OrderReviewedAt, OrderReviewNotes
            // if (order.Status == OrderStatus.ReadyForProduction) { ... } - This depends on business logic for "resetting" fields.
            // For now, we just mark as rejected and add notes.

            order.LastModifiedAt = DateTime.UtcNow;
            order.LastModifiedByUserId = userId;

            var user = await _userService.GetUserByIdAsync(userId);
            await _activityLogService.LogActivityAsync(
                userId: userId,
                userName: user?.UserName ?? $"User ID {userId}",
                action: "RejectOrder",
                details: $"Order '{order.OrderNumber}' (ID: {order.OrderId}) rejected. Reason: {rejectionNotes}",
                targetEntityType: "Order",
                targetEntityId: order.OrderId);

            try
            {
                await _context.SaveChangesAsync();
                _notificationService.ShowSuccess($"Order '{order.OrderNumber}' has been rejected.", "Order Status Updated");
                return true;
            }
            catch (DbUpdateException ex)
            {
                // TODO: Log ex
                _notificationService.ShowError($"Failed to reject order '{order.OrderNumber}'. Database error: {ex.Message}", "Database Error");
                return false;
            }
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
        public async Task<bool> CancelOrderAsync(int orderId, int userId, string? notes)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                _notificationService.ShowError($"Order with ID {orderId} not found.", "Operation Failed");
                return false;
            }

            // Business rule: Order cannot be cancelled if it's already Completed or Cancelled.
            if (order.Status == OrderStatus.Completed)
            {
                _notificationService.ShowWarning(
                    $"Order '{order.OrderNumber}' is already {OrderStatus.Completed.ToString().ToLower()} and cannot be cancelled.",
                    "Action Not Allowed");
                return false;
            }
            if (order.Status == OrderStatus.Cancelled)
            {
                _notificationService.ShowInformation($"Order '{order.OrderNumber}' is already cancelled.", "Order Status");
                return true; // Already in desired state
            }

            order.Status = OrderStatus.Cancelled;

            if (!string.IsNullOrWhiteSpace(notes))
            {
                string cancellationNote = $"Order cancelled by User ID {userId} on {DateTime.UtcNow:yyyy-MM-dd HH:mm}: {notes}";
                order.Notes = string.IsNullOrWhiteSpace(order.Notes)
                    ? cancellationNote
                    : $"{order.Notes}{Environment.NewLine}{cancellationNote}";
            }

            order.LastModifiedAt = DateTime.UtcNow;
            order.LastModifiedByUserId = userId;

            var user = await _userService.GetUserByIdAsync(userId);
            await _activityLogService.LogActivityAsync(
                userId: userId,
                userName: user?.UserName ?? $"User ID {userId}",
                action: "CancelOrder",
                details: $"Order '{order.OrderNumber}' (ID: {order.OrderId}) cancelled. Reason: {notes ?? "N/A"}",
                targetEntityType: "Order",
                targetEntityId: order.OrderId);

            try
            {
                await _context.SaveChangesAsync();
                _notificationService.ShowSuccess($"Order '{order.OrderNumber}' has been cancelled.", "Order Status Updated");
                return true;
            }
            catch (DbUpdateException ex)
            {
                // TODO: Log ex
                _notificationService.ShowError($"Failed to cancel order '{order.OrderNumber}'. Database error: {ex.Message}", "Database Error");
                return false;
            }
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
