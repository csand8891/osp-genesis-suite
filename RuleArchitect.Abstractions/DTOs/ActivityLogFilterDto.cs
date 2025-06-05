// File: RuleArchitect.ApplicationLogic/DTOs/ActivityLogFilterDto.cs
using System;

namespace RuleArchitect.Abstractions.DTOs
{
    /// <summary>
    /// Data Transfer Object for specifying filter criteria when retrieving user activity logs.
    /// </summary>
    public class ActivityLogFilterDto
    {
        /// <summary>
        /// Optional: Filter logs by a specific User ID.
        /// </summary>
        public int? UserIdFilter { get; set; }

        /// <summary>
        /// Optional: Filter logs from this date (inclusive).
        /// </summary>
        public DateTime? DateFrom { get; set; }

        /// <summary>
        /// Optional: Filter logs up to this date (inclusive).
        /// </summary>
        public DateTime? DateTo { get; set; }

        /// <summary>
        /// Optional: Filter by a specific ActivityType string.
        /// </summary>
        public string? ActivityTypeFilter { get; set; }

        /// <summary>
        /// Optional: Filter by a specific TargetEntityType string.
        /// </summary>
        public string? TargetEntityTypeFilter { get; set; }

        /// <summary>
        /// Optional: General search text to be applied against UserName, Description, and TargetEntityDescription.
        /// </summary>
        public string? SearchText { get; set; }

        /// <summary>
        /// For pagination: The page number to retrieve (1-based). Defaults to 1.
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// For pagination: The number of items per page. Defaults to 20 (or a system default).
        /// </summary>
        public int PageSize { get; set; } = 20;
    }
}
