using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuleArchitect.Abstractions.DTOs
{
    /// <summary>
    /// Represents the different types of notifications.
    /// </summary>
    public enum NotificationType
    {
        Information,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// A Data Transfer Object (DTO) carrying details for a notification.
    /// </summary>
    public class NotificationMessage
    {
        /// <summary>
        /// A unique identifier for this specific notification.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// The time the notification was created.
        /// </summary>
        public DateTime Timestamp { get; private set; }

        /// <summary>
        /// The main content of the notification message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The type of notification (Info, Success, Warning, Error).
        /// </summary>
        public NotificationType Type { get; set; } = NotificationType.Information;

        /// <summary>
        /// An optional title for the notification.
        /// </summary>
        public string Title { get; set; } // Nullable reference types aren't 'on' by default here

        /// <summary>
        /// Indicates if the user has viewed/acknowledged this notification in the center.
        /// </summary>
        public bool IsRead { get; set; } = false;

        /// <summary>
        /// Optional text for an action button (e.g., "Undo", "Details").
        /// </summary>
        public string ActionText { get; set; }

        /// <summary>
        /// Optional callback action to execute when the action button is clicked.
        /// </summary>
        public Action<object> ActionCallback { get; set; } // Using 'object' instead of 'object?'

        /// <summary>
        /// Optional argument to pass to the ActionCallback.
        /// </summary>
        public object ActionArgument { get; set; }

        /// <summary>
        /// Optional duration for the notification to be displayed.
        /// Null might mean it persists until dismissed.
        /// </summary>
        public TimeSpan? Duration { get; set; } // TimeSpan? is fine (Nullable<T>)

        /// <summary>
        /// Indicates if the notification requires immediate user attention (e.g., use a dialog).
        /// </summary>
        public bool IsCritical { get; set; } = false;

        /// <summary>
        /// Creates a new NotificationMessage.
        /// </summary>
        /// <param name="message">The main message (cannot be null or empty).</param>
        public NotificationMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException(nameof(message));
            }
            Id = Guid.NewGuid(); // Automatically generate a new ID
            Timestamp = DateTime.UtcNow; // Record creation time (UTC is often best for servers/storage)
            Message = message;
        }
    
    }
}
