// In RuleArchitect/Entities/Notification.cs
using RuleArchitect.Abstractions.DTOs.Notification;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GenesisSentry.Entities; // Add this using directive for UserEntity

namespace RuleArchitect.Entities
{
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        public string Message { get; set; }

        [Required]
        public NotificationType Type { get; set; }

        public string? Title { get; set; }

        [Required]
        public bool IsRead { get; set; }

        [Required]
        public bool IsCritical { get; set; }

        // --- NEW PROPERTIES FOR TARGETING ---

        /// <summary>
        /// The ID of the user this notification is for.
        /// If NULL, it's a system-wide or role-based notification.
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// The role this notification is targeted at.
        /// If NULL, it's a user-specific or system-wide notification.
        /// </summary>
        [MaxLength(100)]
        public string? Role { get; set; }

        /// <summary>
        /// Navigation property to the User.
        /// </summary>
        [ForeignKey("UserId")]
        public virtual UserEntity? User { get; set; }
    }
}