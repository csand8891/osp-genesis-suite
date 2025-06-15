using HeraldKit.Interfaces;
using RuleArchitect.Abstractions.DTOs.Notification;
using MaterialDesignThemes.Wpf;
using System;
using System.Windows;

namespace RuleArchitect.DesktopClient.Services
{
    public class WpfNotificationService : INotificationService
    {
        private readonly SnackbarMessageQueue _snackbarMessageQueue;

        public WpfNotificationService(SnackbarMessageQueue snackbarMessageQueue)
        {
            _snackbarMessageQueue = snackbarMessageQueue ?? throw new ArgumentNullException(nameof(snackbarMessageQueue));
        }

        private void EnqueueMessage(string message, TimeSpan? duration = null)
        {
            if (Application.Current?.Dispatcher == null || Application.Current.Dispatcher.CheckAccess())
            {
                _snackbarMessageQueue.Enqueue(message, null, null, null, false, true, duration);
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                    _snackbarMessageQueue.Enqueue(message, null, null, null, false, true, duration));
            }
        }

        public void ShowError(string message, string title = "Error", bool isCritical = false)
        {
            // Snackbar doesn't have different colors, but we can make errors last longer.
            EnqueueMessage($"ERROR: {message}", isCritical ? TimeSpan.FromSeconds(5) : (TimeSpan?)null);
        }

        public void ShowInformation(string message, string title = "Information", TimeSpan? duration = null)
        {
            EnqueueMessage(message, duration);
        }

        public void ShowNotification(NotificationMessage notification)
        {
            if (notification == null) return;
            EnqueueMessage(notification.Message, notification.Duration);
        }

        public void ShowSuccess(string message, string title = "Success", TimeSpan? duration = null)
        {
            EnqueueMessage(message, duration);
        }

        public void ShowWarning(string message, string title = "Warning", TimeSpan? duration = null)
        {
            EnqueueMessage($"Warning: {message}", duration);
        }
    }
}
