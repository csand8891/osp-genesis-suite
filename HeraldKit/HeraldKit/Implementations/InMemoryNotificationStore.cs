using HeraldKit.Interfaces; // Ensure you have the 's' if your folder/namespace is 'Interaces'
using RuleArchitect.Abstractions.DTOs.Notification;
using RuleArchitect.Abstractions.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HeraldKit.Implementations
{  // Or HeraldKit.Implementations if you created that sub-folder/namespace

    /// <summary>
    /// A simple, non-persistent implementation of INotificationStore using an in-memory list.
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

        public IEnumerable<NotificationMessage> GetAll()
        {
            lock (_lock)
            {
                // Return a copy to prevent external modification issues
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

        public int GetUnreadCount()
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
    }
}