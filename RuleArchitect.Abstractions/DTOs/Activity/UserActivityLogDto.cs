// File: RuleArchitect.ApplicationLogic/DTOs/UserActivityLogDto.cs
using System;

namespace RuleArchitect.Abstractions.DTOs.Activity
{
    /// <summary>
    /// Data Transfer Object for representing a user activity log entry.
    /// </summary>
    public class UserActivityLogDto
    {
        public long UserActivityLogId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public string? TargetEntityType { get; set; }
        public int? TargetEntityId { get; set; }
        public string? TargetEntityDescription { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        // The 'Details' field from the entity is omitted here for brevity in the list view,
        // but could be included if a detailed view of a log entry is required.
        // public string? Details { get; set; }

        // Example of a formatted timestamp string if needed directly in DTO for UI
        // public string TimestampFormatted => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
    }
}