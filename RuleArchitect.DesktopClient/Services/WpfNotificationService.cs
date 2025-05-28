// File: RuleArchitect.DesktopClient/Services/WpfNotificationService.cs
using HeraldKit.Interfaces; // Ensure this matches the namespace in your HeraldKit project
using HeraldKit.Models;
using System;
using System.Linq; // For .OfType<Window>().FirstOrDefault()
using System.Windows;

namespace RuleArchitect.DesktopClient.Services // Or your chosen namespace for client-side services
{
    public class WpfNotificationService : INotificationService // Implements the correct interface
    {
        private void ShowMessage(string message, string title, MessageBoxImage icon)
        {
            if (Application.Current?.Dispatcher == null || Application.Current.Dispatcher.CheckAccess())
            {
                // If on UI thread or dispatcher is null (e.g. during shutdown tests)
                DoShowMessage(message, title, icon);
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() => DoShowMessage(message, title, icon));
            }
        }

        private void DoShowMessage(string message, string title, MessageBoxImage icon)
        {
            Window owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive && x.IsVisible) ?? Application.Current?.MainWindow;
            MessageBox.Show(owner ?? Application.Current?.MainWindow, message, title, MessageBoxButton.OK, icon);
        }


        public void ShowError(string message, string title = null, bool isCritical = false)
        {
            ShowMessage(message, title ?? "Error", MessageBoxImage.Error);
        }

        public void ShowInformation(string message, string title = null, TimeSpan? duration = null)
        {
            ShowMessage(message, title ?? "Information", MessageBoxImage.Information);
        }

        public void ShowNotification(NotificationMessage notification)
        {
            if (notification == null) return;

            string effectiveTitle = notification.Title ?? notification.Type.ToString();
            MessageBoxImage image = MessageBoxImage.None;
            switch (notification.Type)
            {
                case NotificationType.Information: image = MessageBoxImage.Information; break;
                case NotificationType.Success: image = MessageBoxImage.Information; break;
                case NotificationType.Warning: image = MessageBoxImage.Warning; break;
                case NotificationType.Error: image = MessageBoxImage.Error; break;
            }
            ShowMessage(notification.Message, effectiveTitle, image);
        }

        public void ShowSuccess(string message, string title = null, TimeSpan? duration = null)
        {
            ShowMessage(message, title ?? "Success", MessageBoxImage.Information);
        }

        public void ShowWarning(string message, string title = null, TimeSpan? duration = null)
        {
            ShowMessage(message, title ?? "Warning", MessageBoxImage.Warning);
        }
    }
}