// In HeraldKit/Implementations/DatabaseNotificationStore.cs
using HeraldKit.Interfaces;
using RuleArchitect.Abstractions.DTOs.Auth;
using RuleArchitect.Abstractions.DTOs.Notification;
using RuleArchitect.Abstractions.Interfaces;
using RuleArchitect.Data;
using RuleArchitect.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HeraldKit.Implementations
{
    public class DatabaseNotificationStore : INotificationStore
    {
        private readonly IServiceProvider _serviceProvider;

        public event EventHandler<StoreChangedEventArgs> StoreChanged;

        public DatabaseNotificationStore(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Helper to get a fresh DbContext for each operation
        private RuleArchitectContext GetContext() =>
            ((Microsoft.Extensions.DependencyInjection.IServiceScope)_serviceProvider.GetService(typeof(Microsoft.Extensions.DependencyInjection.IServiceScopeFactory))).ServiceProvider.GetRequiredService<RuleArchitectContext>();

        public void Add(NotificationMessage message)
        {
            using (var context = GetContext())
            {
                var entity = new Notification
                {
                    Id = message.Id,
                    Timestamp = message.Timestamp,
                    Message = message.Message,
                    Type = message.Type,
                    Title = message.Title,
                    IsRead = message.IsRead,
                    IsCritical = message.IsCritical,
                    // By default, this is a system-wide notification
                    UserId = null,
                    Role = null
                };
                context.Notifications.Add(entity);
                context.SaveChanges();
            }
            StoreChanged?.Invoke(this, new StoreChangedEventArgs(StoreChangeAction.Added, message.Id));
        }

        public IEnumerable<NotificationMessage> GetAll(UserDto user)
        {
            if (user == null) return Enumerable.Empty<NotificationMessage>();

            using (var context = GetContext())
            {
                return context.Notifications
                    .Where(n =>
                        n.UserId == null && n.Role == null || // System-wide
                        n.UserId == user.UserId ||             // User-specific
                        n.Role == user.Role)                   // Role-specific
                    .OrderByDescending(n => n.Timestamp)
                    .Select(n => MapToMessage(n))
                    .ToList();
            }
        }

        public bool Remove(Guid messageId)
        {
            using (var context = GetContext())
            {
                var entity = context.Notifications.Find(messageId);
                if (entity != null)
                {
                    context.Notifications.Remove(entity);
                    context.SaveChanges();
                    StoreChanged?.Invoke(this, new StoreChangedEventArgs(StoreChangeAction.Removed, messageId));
                    return true;
                }
                return false;
            }
        }

        public void ClearAll()
        {
            using (var context = GetContext())
            {
                var allIds = context.Notifications.Select(n => n.Id).ToList();
                if (allIds.Any())
                {
                    context.Notifications.RemoveRange(context.Notifications);
                    context.SaveChanges();
                    StoreChanged?.Invoke(this, new StoreChangedEventArgs(StoreChangeAction.Cleared, allIds));
                }
            }
        }

        public NotificationMessage GetById(Guid messageId)
        {
            using (var context = GetContext())
            {
                var entity = context.Notifications.Find(messageId);
                return entity != null ? MapToMessage(entity) : null;
            }
        }

        public int GetUnreadCount(UserDto user)
        {
            if (user == null) return 0;
            using (var context = GetContext())
            {
                return context.Notifications.Count(n => !n.IsRead &&
                    (n.UserId == null && n.Role == null ||
                     n.UserId == user.UserId ||
                     n.Role == user.Role));
            }
        }

        public bool MarkAsRead(Guid messageId)
        {
            using (var context = GetContext())
            {
                var entity = context.Notifications.Find(messageId);
                if (entity != null && !entity.IsRead)
                {
                    entity.IsRead = true;
                    context.SaveChanges();
                    StoreChanged?.Invoke(this, new StoreChangedEventArgs(StoreChangeAction.Updated, messageId));
                    return true;
                }
                return false;
            }
        }

        public int MarkAllAsRead()
        {
            using (var context = GetContext())
            {
                var unreadNotifications = context.Notifications.Where(n => !n.IsRead).ToList();
                if (unreadNotifications.Any())
                {
                    var ids = new List<Guid>();
                    foreach (var notification in unreadNotifications)
                    {
                        notification.IsRead = true;
                        ids.Add(notification.Id);
                    }
                    context.SaveChanges();
                    StoreChanged?.Invoke(this, new StoreChangedEventArgs(StoreChangeAction.Updated, ids));
                    return unreadNotifications.Count;
                }
                return 0;
            }
        }

        private NotificationMessage MapToMessage(Notification entity)
        {
            var message = new NotificationMessage(entity.Message)
            {
                Title = entity.Title,
                Type = entity.Type,
                IsRead = entity.IsRead,
                IsCritical = entity.IsCritical,
            };
            // Use reflection to set the private Id and Timestamp properties
            typeof(NotificationMessage).GetProperty("Id").SetValue(message, entity.Id);
            typeof(NotificationMessage).GetProperty("Timestamp").SetValue(message, entity.Timestamp);
            return message;
        }
    }
}