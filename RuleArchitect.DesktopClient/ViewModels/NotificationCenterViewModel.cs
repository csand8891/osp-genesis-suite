// In RuleArchitect.DesktopClient/ViewModels/NotificationCenterViewModel.cs
using RuleArchitect.Abstractions.DTOs.Notification;
using RuleArchitect.Abstractions.Interfaces;
using RuleArchitect.DesktopClient.Commands;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace RuleArchitect.DesktopClient.ViewModels
{
    public class NotificationCenterViewModel : BaseViewModel
    {
        private readonly INotificationStore _notificationStore;
        private readonly IAuthenticationStateProvider _authStateProvider;

        public ObservableCollection<NotificationMessage> Notifications { get; }

        public ICommand MarkAsReadCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand ClearAllCommand { get; }

        public NotificationCenterViewModel(INotificationStore notificationStore, IAuthenticationStateProvider authStateProvider)
        {
            _notificationStore = notificationStore;
            _authStateProvider = authStateProvider;
            Notifications = new ObservableCollection<NotificationMessage>();

            MarkAsReadCommand = new RelayCommand(ExecuteMarkAsRead, (p) => p is NotificationMessage);
            RemoveCommand = new RelayCommand(ExecuteRemove, (p) => p is NotificationMessage);
            ClearAllCommand = new RelayCommand(ExecuteClearAll);

            LoadNotifications();

            // Refresh the view whenever the store changes
            _notificationStore.StoreChanged += (s, e) => LoadNotifications();
        }

        private void LoadNotifications()
        {
            var currentUser = _authStateProvider.CurrentUser;
            if (currentUser == null) return;

            Notifications.Clear();
            var messages = _notificationStore.GetAll(currentUser);
            foreach (var message in messages.OrderByDescending(m => m.Timestamp))
            {
                Notifications.Add(message);
            }
        }

        private void ExecuteMarkAsRead(object parameter)
        {
            if (parameter is NotificationMessage message)
            {
                _notificationStore.MarkAsRead(message.Id);
                // The StoreChanged event will trigger a reload, automatically updating the UI.
            }
        }

        private void ExecuteRemove(object parameter)
        {
            if (parameter is NotificationMessage message)
            {
                _notificationStore.Remove(message.Id);
            }
        }

        private void ExecuteClearAll()
        {
            _notificationStore.ClearAll();
        }
    }
}