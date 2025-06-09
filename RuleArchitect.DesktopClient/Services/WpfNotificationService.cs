// File: RuleArchitect.DesktopClient/Services/WpfNotificationService.cs
using HeraldKit.Interfaces;
using RuleArchitect.Abstractions.DTOs;
using MaterialDesignThemes.Wpf; // Add this
using System;
using System.Windows; // For Application.Current.Dispatcher if needed

namespace RuleArchitect.DesktopClient.Services
{
    public class WpfNotificationService : INotificationService
    {
        private readonly SnackbarMessageQueue _snackbarMessageQueue;

        public WpfNotificationService(SnackbarMessageQueue snackbarMessageQueue) // Inject the queue
        {
            _snackbarMessageQueue = snackbarMessageQueue ?? throw new ArgumentNullException(nameof(snackbarMessageQueue));
        }

        private void EnqueueMessage(string message, string? actionContent = null, Action<object?>? actionHandler = null, object? actionArgument = null, TimeSpan? duration = null, bool promote = false)
        {
            // Ensure execution on the UI thread if called from a background thread
            if (Application.Current?.Dispatcher == null || Application.Current.Dispatcher.CheckAccess())
            {
                _snackbarMessageQueue.Enqueue(message, actionContent, actionHandler, actionArgument, promote, true, duration);
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                    _snackbarMessageQueue.Enqueue(message, actionContent, actionHandler, actionArgument, promote, true, duration));
            }
        }

        public void ShowError(string message, string title = "Error", bool isCritical = false)
        {
            // For critical errors, you might still want a MessageBox, or a Snackbar that stays longer / has different styling.
            // For now, all will go to Snackbar.
            EnqueueMessage($"{title}: {message}", duration: isCritical ? TimeSpan.FromSeconds(5) : (TimeSpan?)null);
        }

        public void ShowInformation(string message, string title = "Information", TimeSpan? duration = null)
        {
            EnqueueMessage(string.IsNullOrEmpty(title) ? message : $"{title}: {message}", duration: duration);
        }

        public void ShowNotification(NotificationMessage notification)
        {
            if (notification == null) return;

            string effectiveTitle = notification.Title ?? notification.Type.ToString();
            string fullMessage = string.IsNullOrEmpty(notification.Title) ? notification.Message : $"{notification.Title}: {notification.Message}";

            // Simplified: Ignoring ActionText/Callback for this example, can be added.
            EnqueueMessage(fullMessage, duration: notification.Duration, promote: notification.IsCritical);
        }

        public void ShowSuccess(string message, string title = "Success", TimeSpan? duration = null)
        {
            EnqueueMessage(string.IsNullOrEmpty(title) ? message : $"{title}: {message}", duration: duration);
        }

        public void ShowWarning(string message, string title = "Warning", TimeSpan? duration = null)
        {
            EnqueueMessage(string.IsNullOrEmpty(title) ? message : $"{title}: {message}", duration: duration);
        }
    }
}