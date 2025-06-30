// In HeraldKit/Implementations/InMemoryNotificationStore.cs
using HeraldKit.Interfaces;
using RuleArchitect.Abstractions.DTOs.Auth;
using RuleArchitect.Abstractions.DTOs.Notification;
using RuleArchitect.Abstractions.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HeraldKit.Implementations
{
    /// <summary>
    /// A simple, non-persistent implementation of INotificationStore using an in-memory list.
    /// NOTE: This implementation does not filter by user and returns all notifications. It is suitable for scenarios where only one user is logged in at a time or for system-wide notifications.
    /// </summary>
    public class InMemoryNotificationStore : INotificationStore
    {
        private readonly List<NotificationMessage> _notifications = new List<NotificationMessage>();
        private readonly object _lock = new object(); // Basic thread safety for list modifications

        public event EventHandler<StoreChangedEventArgs> StoreChanged;

        public void Add(NotificationMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            lock (_lock)
            {
                _notifications.Add(message);
            }
            StoreChanged?.Invoke(this, new StoreChangedEventArgs(StoreChangeAction.Added, message.Id));
        }

        public bool Remove(Guid messageId)
        {
            bool removed = false;
            lock (_lock)
            {
                var messageToRemove = _notifications.FirstOrDefault(m => m.Id == messageId);
                if (messageToRemove != null)
                {
                    _notifications.Remove(messageToRemove);
                    removed = true;
                }
            }
            if (removed)
            {
                StoreChanged?.Invoke(this, new StoreChangedEventArgs(StoreChangeAction.Removed, messageId));
            }
            return removed;
        }

        public void ClearAll()
        {
            List<Guid> ids;
            lock (_lock)
            {
                ids = _notifications.Select(n => n.Id).ToList();
                _notifications.Clear();
            }
            if (ids.Any())
            {
                StoreChanged?.Invoke(this, new StoreChangedEventArgs(StoreChangeAction.Cleared, ids));
            }
        }

        /// <summary>
        /// Retrieves all current notifications from the store. This in-memory implementation does not filter by user.
        /// </summary>
        public IEnumerable<NotificationMessage> GetAll(UserDto user)
        {
            lock (_lock)
            {
                // This is an in-memory store, so for simplicity, we return all notifications
                // regardless of the user. A more complex implementation could handle user-specific messages.
                return _notifications.ToList();
            }
        }

        public NotificationMessage GetById(Guid messageId)
        {
            lock (_lock)
            {
                return _notifications.FirstOrDefault(m => m.Id == messageId);
            }
        }

        /// <summary>
        /// Gets the count of all unread notifications. This in-memory implementation does not filter by user.
        /// </summary>
        public int GetUnreadCount(UserDto user)
        {
            lock (_lock)
            {
                return _notifications.Count(m => !m.IsRead);
            }
        }

        public bool MarkAsRead(Guid messageId)
        {
            NotificationMessage msg;
            lock (_lock)
            {
                msg = _notifications.FirstOrDefault(m => m.Id == messageId);
                if (msg != null && !msg.IsRead)
                {
                    msg.IsRead = true;
                }
                else
                {
                    msg = null; // Ensure no event if not found or already read
                }
            }

            if (msg != null)
            {
                StoreChanged?.Invoke(this, new StoreChangedEventArgs(StoreChangeAction.Updated, messageId));
                return true;
            }
            return false;
        }

        public int MarkAllAsRead()
        {
            List<Guid> updatedIds = new List<Guid>();
            lock (_lock)
            {
                foreach (var msg in _notifications)
                {
                    if (!msg.IsRead)
                    {
                        msg.IsRead = true;
                        updatedIds.Add(msg.Id);
                    }
                }
            }

            if (updatedIds.Any())
            {
                StoreChanged?.Invoke(this, new StoreChangedEventArgs(StoreChangeAction.Updated, updatedIds));
            }
            return updatedIds.Count;
        }

        // Explicitly implementing the old parameter-less GetAll method to satisfy any remaining dependencies,
        // although the interface now requires the version with UserDto.
        public IEnumerable<NotificationMessage> GetAll()
        {
            return this.GetAll(null); // Delegate to the new method, passing null.
        }

        public int GetUnreadCount()
        {
            return this.GetUnreadCount(null); // Delegate to the new method, passing null.
        }
    }
}