using Microsoft.EntityFrameworkCore;
using RuleArchitect.Abstractions.DTOs;
using RuleArchitect.Abstractions.DTOs.Activity;
using RuleArchitect.Abstractions.Interfaces;
using RuleArchitect.Data;
using RuleArchitect.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuleArchitect.ApplicationLogic.Services
{
    public class UserActivityLogService : IUserActivityLogService
    {
        private readonly RuleArchitectContext _context;

        public UserActivityLogService(RuleArchitectContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task LogActivityAsync(
            int userId,
            string userName,
            string activityType,
            string description,
            bool saveChanges = true,
            string? targetEntityType = null,
            int? targetEntityId = null,
            string? targetEntityDescription = null,
            string? details = null,
            string? ipAddress = null)
        {
            if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentException("UserName cannot be empty.", nameof(userName));
            if (string.IsNullOrWhiteSpace(activityType)) throw new ArgumentException("ActivityType cannot be empty.", nameof(activityType));
            if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description cannot be empty.", nameof(description));

            var logEntry = new UserActivityLog
            {
                UserId = userId,
                UserName = userName,
                Timestamp = DateTime.UtcNow,
                ActivityType = activityType,
                TargetEntityType = targetEntityType,
                TargetEntityId = targetEntityId,
                TargetEntityDescription = targetEntityDescription,
                Description = description,
                Details = details,
                IpAddress = ipAddress
            };

            _context.UserActivityLogs.Add(logEntry);

            if (saveChanges)
            {
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<UserActivityLogDto>> GetActivityLogsAsync(ActivityLogFilterDto filterDto)
        {
            var query = _context.UserActivityLogs.AsNoTracking();

            if (filterDto.UserIdFilter.HasValue)
            {
                query = query.Where(log => log.UserId == filterDto.UserIdFilter.Value);
            }
            if (filterDto.DateFrom.HasValue)
            {
                query = query.Where(log => log.Timestamp >= filterDto.DateFrom.Value);
            }
            if (filterDto.DateTo.HasValue)
            {
                var dateTo = filterDto.DateTo.Value.Date.AddDays(1);
                query = query.Where(log => log.Timestamp < dateTo);
            }
            if (!string.IsNullOrWhiteSpace(filterDto.ActivityTypeFilter))
            {
                query = query.Where(log => log.ActivityType == filterDto.ActivityTypeFilter);
            }
            if (!string.IsNullOrWhiteSpace(filterDto.SearchText))
            {
                string searchTextLower = filterDto.SearchText.ToLower();
                query = query.Where(log => (log.Description != null && log.Description.ToLower().Contains(searchTextLower)) ||
                                           (log.TargetEntityDescription != null && log.TargetEntityDescription.ToLower().Contains(searchTextLower)) ||
                                           (log.UserName != null && log.UserName.ToLower().Contains(searchTextLower)));
            }

            return await query.OrderByDescending(log => log.Timestamp)
                              .Select(log => new UserActivityLogDto
                              {
                                  UserActivityLogId = log.UserActivityLogId,
                                  UserId = log.UserId,
                                  UserName = log.UserName,
                                  Timestamp = log.Timestamp,
                                  ActivityType = log.ActivityType,
                                  TargetEntityType = log.TargetEntityType,
                                  TargetEntityId = log.TargetEntityId,
                                  TargetEntityDescription = log.TargetEntityDescription,
                                  Description = log.Description,
                                  IpAddress = log.IpAddress
                              })
                              .ToListAsync();
        }

        /// <summary>
        /// Retrieves a list of all distinct activity types from the logs.
        /// </summary>
        /// <returns>A list of unique activity type strings, ordered alphabetically.</returns>
        public async Task<List<string>> GetDistinctActivityTypesAsync()
        {
            return await _context.UserActivityLogs
                .AsNoTracking()
                .Select(log => log.ActivityType)
                // ADDED: Filter out any potential null values before sorting
                .Where(type => type != null)
                .Distinct()
                .OrderBy(type => type)
                .ToListAsync();
        }
    }
}
