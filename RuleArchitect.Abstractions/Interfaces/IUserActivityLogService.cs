using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RuleArchitect.ApplicationLogic.DTOs;
using RuleArchitect.Entities;


namespace RuleArchitect.Abstractions.Interfaces
{
    public interface IUserActivityLogService
    {
        /// <summary>
        /// Logs a user activity.
        /// </summary>
        /// <param name="userId">The ID of the user performing the action.</param>
        /// <param name="userName">The username of the user.</param>
        /// <param name="activityType">The type of activity (e.g., "UserLogin", "CreateSoftwareOption").</param>
        /// <param name="description">A human-readable description of the activity.</param>
        /// <param name="targetEntityType">Optional: The type of entity affected (e.g., "SoftwareOption").</param>
        /// <param name="targetEntityId">Optional: The ID of the entity affected.</param>
        /// <param name="targetEntityDescription">Optional: A short description of the target entity.</param>
        /// <param name="details">Optional: Additional details about the activity (e.g., JSON of changes).</param>
        /// <param name="ipAddress">Optional: The IP address of the user.</param>
        Task LogActivityAsync(
            int userId,
            string userName,
            string activityType,
            string description,
            string? targetEntityType = null,
            int? targetEntityId = null,
            string? targetEntityDescription = null,
            string? details = null,
            string? ipAddress = null);

        /// <summary>
        /// Retrieves a paginated and filterable list of user activities.
        /// </summary>
        /// <param name="filterDto">DTO containing filter criteria (e.g., UserID, DateRange, ActivityType).</param>
        /// <returns>A list of UserActivityLog DTOs or entities.</returns>
        Task<IEnumerable<UserActivityLogDto>> GetActivityLogsAsync(ActivityLogFilterDto filterDto); // You'll need to define UserActivityLogDto and ActivityLogFilterDto

    }
}
