using HeraldKit.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeraldKit.Interfaces
{
    /// <summary>
    /// Defines a contract for storing and managing user notifications.
    /// </summary>
    public interface INotificationStore
    {
        /// <summary>
        /// Occurs when the notification store's content changes.
        /// </summary>
        event EventHandler<StoreChangedEventArgs> StoreChanged;

        /// <summary>
        /// Adds a new notification to the store.
        /// </summary>
        /// <param name="message">The notification message to add.</param>
        void Add(NotificationMessage message);

        /// <summary>
        /// Removes a notification from the store by its ID.
        /// </summary>
        /// <param name="messageId">The Guid of the notification to remove.</param>
        /// <returns>True if the item was found and removed, otherwise false.</returns>
        bool Remove(Guid messageId);

        /// <summary>
        /// Removes all notifications from the store.
        /// </summary>
        void ClearAll();

        /// <summary>
        /// Retrieves all current notifications from the store.
        /// </summary>
        /// <returns>An enumerable collection of NotificationMessage.</returns>
        IEnumerable<NotificationMessage> GetAll();

        /// <summary>
        /// Gets a specific notification by its ID.
        /// </summary>
        /// <param name="messageId">The ID to search for.</param>
        /// <returns>The NotificationMessage or null if not found.</returns>
        NotificationMessage GetById(Guid messageId);

        /// <summary>
        /// Gets the count of unread notifications.
        /// </summary>
        /// <returns>The number of unread notifications.</returns>
        int GetUnreadCount();

        /// <summary>
        /// Marks a specific notification as read.
        /// </summary>
        /// <param name="messageId">The Guid of the notification to mark as read.</param>
        /// <returns>True if the item was found and marked, otherwise false.</returns>
        bool MarkAsRead(Guid messageId);

        /// <summary>
        /// Marks all notifications as read.
        /// </summary>
        /// <returns>The number of items marked as read.</returns>
        int MarkAllAsRead();
    }
}
