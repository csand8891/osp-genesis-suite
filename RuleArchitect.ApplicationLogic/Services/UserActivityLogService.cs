using Microsoft.EntityFrameworkCore;
using RuleArchitect.Abstractions.DTOs;
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
            // Note: SaveChangesAsync will typically be called by the service that *initiated* the overall operation
            // (e.g., SoftwareOptionService after successfully saving a SoftwareOption and its log).
            // However, for some logs like login, this service might be the one calling SaveChanges.
            // If this service is called within an existing transaction/DbContext scope,
            // adding the log entry and letting the parent service save is fine.
            // If it's a standalone log (like login), this service should save.
            // For simplicity here, we'll assume the caller handles SaveChanges or this is a specific log call that needs saving.
            // Consider if SaveChanges should be called here or by the consuming service.
            // A common pattern is for the primary service (e.g. OrderService) to manage the transaction and SaveChanges.
            // For now, let's add it for standalone logging capability.
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<UserActivityLogDto>> GetActivityLogsAsync(ActivityLogFilterDto filterDto)
        {
            // You would define ActivityLogFilterDto with properties like:
            // int? UserIdFilter { get; set; }
            // DateTime? DateFrom { get; set; }
            // DateTime? DateTo { get; set; }
            // string? ActivityTypeFilter { get; set; }
            // string? SearchText { get; set; } // For Description or TargetEntityDescription
            // int PageNumber { get; set; } = 1;
            // int PageSize { get; set; } = 20;

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
                // Add 1 day to DateTo to make the range inclusive for the entire day
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

            // Implement pagination
            // query = query.Skip((filterDto.PageNumber - 1) * filterDto.PageSize).Take(filterDto.PageSize);


            return await query.OrderByDescending(log => log.Timestamp)
                              .Select(log => new UserActivityLogDto // You need to define UserActivityLogDto
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
                                  // Map other fields as needed for display
                              })
                              .ToListAsync();
        }
    }
}
