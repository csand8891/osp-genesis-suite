using System;
using System.Collections.Generic; // For IEnumerable
using System.Threading.Tasks;
using RuleArchitect.Abstractions.DTOs; // Assuming OrderDetailDto, CreateOrderDto, UpdateOrderDto are here
using RuleArchitect.Abstractions.Enums; // For OrderStatus enum

namespace RuleArchitect.Abstractions.Interfaces
{
    /// <summary>
    /// Defines the contract for services managing orders and their lifecycle within the application.
    /// This includes creation, retrieval, updates, and various status transitions reflecting the order workflow.
    /// </summary>
    public interface IOrderService
    {
        /// <summary>
        /// Creates a new order in the system.
        /// </summary>
        /// <param name="createOrderDto">The data transfer object containing details for the new order.</param>
        /// <param name="createdByUserId">The ID of the user creating the order.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result contains the <see cref="OrderDetailDto"/> of the newly created order.</returns>
        Task<OrderDetailDto> CreateOrderAsync(CreateOrderDto createOrderDto, int createdByUserId);

        /// <summary>
        /// Retrieves a specific order by its unique identifier.
        /// </summary>
        /// <param name="orderId">The unique ID of the order to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation.
        /// The task result contains the <see cref="OrderDetailDto"/> if found; otherwise, null.</returns>
        Task<OrderDetailDto?> GetOrderByIdAsync(int orderId);

        /// <summary>
        /// Updates an existing order with the provided details.
        /// </summary>
        /// <param name="orderId">The unique ID of the order to update.</param>
        /// <param name="updateOrderDto">The data transfer object containing the updated order details.</param>
        /// <param name="modifiedByUserId">The ID of the user performing the update.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result contains the updated <see cref="OrderDetailDto"/> if the update was successful; otherwise, null (e.g., if the order was not found).</returns>
        Task<OrderDetailDto?> UpdateOrderAsync(int orderId, UpdateOrderDto updateOrderDto, int modifiedByUserId);

        /// <summary>
        /// Removes a specific software option (line item) from an order.
        /// </summary>
        /// <param name="orderId">The ID of the order from which to remove the item.</param>
        /// <param name="orderItemId">The ID of the order item (linking to the software option) to remove.</param>
        /// <param name="removedByUserId">The ID of the user performing the removal.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result is true if the item was successfully removed; otherwise, false.</returns>
        Task<bool> RemoveSoftwareOptionFromOrderAsync(int orderId, int orderItemId, int removedByUserId);

        /// <summary>
        /// Submits an order for production, typically after initial creation and review.
        /// Updates the order status and records the reviewer.
        /// </summary>
        /// <param name="orderId">The ID of the order to submit.</param>
        /// <param name="reviewerUserId">The ID of the user submitting/reviewing the order for production.</param>
        /// <param name="notes">Optional notes for this status change.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result is true if the order was successfully submitted; otherwise, false.</returns>
        Task<bool> SubmitOrderForProductionAsync(int orderId, int reviewerUserId, string? notes);

        /// <summary>
        /// Marks an order as having started production.
        /// Updates the order status and records the production technician.
        /// </summary>
        /// <param name="orderId">The ID of the order.</param>
        /// <param name="techUserId">The ID of the production technician starting the work.</param>
        /// <param name="notes">Optional notes for this status change.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result is true if the production status was successfully updated; otherwise, false.</returns>
        Task<bool> StartProductionAsync(int orderId, int techUserId, string? notes);

        /// <summary>
        /// Marks an order's production phase as complete.
        /// Updates the order status and records the production technician.
        /// </summary>
        /// <param name="orderId">The ID of the order.</param>
        /// <param name="techUserId">The ID of the production technician completing the work.</param>
        /// <param name="notes">Optional notes for this status change.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result is true if the production completion status was successfully updated; otherwise, false.</returns>
        Task<bool> CompleteProductionAsync(int orderId, int techUserId, string? notes);

        /// <summary>
        /// Marks an order as having started the software review phase.
        /// Updates the order status and records the software reviewer.
        /// </summary>
        /// <param name="orderId">The ID of the order.</param>
        /// <param name="reviewerUserId">The ID of the user performing the software review.</param>
        /// <param name="notes">Optional notes for this status change.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result is true if the software review status was successfully updated; otherwise, false.</returns>
        Task<bool> StartSoftwareReviewAsync(int orderId, int reviewerUserId, string? notes);

        /// <summary>
        /// Rejects an order at some stage in its workflow, setting it to a specified 'rejected' status.
        /// </summary>
        /// <param name="orderId">The ID of the order to reject.</param>
        /// <param name="newStatus">The specific <see cref="OrderStatus"/> to set (e.g., OrderRejected, ReviewRejected). It must be a status that indicates rejection.</param>
        /// <param name="userId">The ID of the user rejecting the order.</param>
        /// <param name="rejectionNotes">Mandatory notes explaining the reason for rejection.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result is true if the order was successfully rejected; otherwise, false.</returns>
        Task<bool> RejectOrderAsync(int orderId, OrderStatus newStatus, int userId, string rejectionNotes);

        /// <summary>
        /// Cancels an order.
        /// Updates the order status to a 'Cancelled' state.
        /// </summary>
        /// <param name="orderId">The ID of the order to cancel.</param>
        /// <param name="userId">The ID of the user cancelling the order.</param>
        /// <param name="notes">Optional notes explaining the reason for cancellation.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result is true if the order was successfully cancelled; otherwise, false.</returns>
        Task<bool> CancelOrderAsync(int orderId, int userId, string? notes);

        /// <summary>
        /// Puts an order on hold.
        /// Updates the order status to an 'OnHold' state.
        /// </summary>
        /// <param name="orderId">The ID of the order to put on hold.</param>
        /// <param name="userId">The ID of the user putting the order on hold.</param>
        /// <param name="notes">Optional notes explaining the reason for the hold.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result is true if the order was successfully put on hold; otherwise, false.</returns>
        Task<bool> PutOrderOnHoldAsync(int orderId, int userId, string? notes);

        
        Task<IEnumerable<OrderDetailDto>> GetAllOrdersAsync(OrderFilterDto filterDto);
        
    }
}