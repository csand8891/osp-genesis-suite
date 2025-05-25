using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeraldKit.Models;

namespace HeraldKit.Interaces
{
    /// <summary>
    /// Defines a contract for services that can display notifications to the user.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Shows an informational message.
        /// </summary>
        void ShowInformation(string message, string title = null, TimeSpan? duration = null);

        /// <summary>
        /// Shows a success message.
        /// </summary>
        void ShowSuccess(string message, string title = null, TimeSpan? duration = null);

        /// <summary>
        /// Shows a warning message.
        /// </summary>
        void ShowWarning(string message, string title = null, TimeSpan? duration = null);

        /// <summary>
        /// Shows an error message.
        /// </summary>
        void ShowError(string message, string title = null, bool isCritical = false);

        /// <summary>
        /// Shows a notification using a structured message object.
        /// </summary>
        void ShowNotification(NotificationMessage notification);
    }
}
