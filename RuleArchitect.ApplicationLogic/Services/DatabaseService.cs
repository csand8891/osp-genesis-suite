// In RuleArchitect.ApplicationLogic/Services/DatabaseService.cs
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RuleArchitect.Abstractions.Interfaces;
using RuleArchitect.Data;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RuleArchitect.ApplicationLogic.Services
{
    /// <summary>
    /// Provides services for database management, such as backup and restore operations.
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        private readonly RuleArchitectContext _context;
        private readonly IUserActivityLogService _activityLogService;

        public DatabaseService(RuleArchitectContext context, IUserActivityLogService activityLogService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _activityLogService = activityLogService ?? throw new ArgumentNullException(nameof(activityLogService));
        }

        /// <summary>
        /// Backs up the active SQLite database and logs the activity.
        /// </summary>
        /// <param name="destinationFilePath">The full path for the backup file.</param>
        /// <param name="userId">The ID of the user performing the action.</param>
        /// <param name="userName">The username of the user performing the action.</param>
        /// <returns>True if the backup file was created successfully; otherwise, false.</returns>
        public async Task<bool> BackupDatabaseAsync(string destinationFilePath, int userId, string userName)
        {
            try
            {
                var connectionString = _context.Database.GetDbConnection().ConnectionString;
                var sqliteConnectionStringBuilder = new SqliteConnectionStringBuilder(connectionString);
                var sourceFilePath = sqliteConnectionStringBuilder.DataSource;

                if (!File.Exists(sourceFilePath))
                {
                    Console.WriteLine($"Database source file not found at: {sourceFilePath}");
                    return false;
                }

                await _context.Database.CloseConnectionAsync();

                var destinationDirectory = Path.GetDirectoryName(destinationFilePath);
                if (!string.IsNullOrEmpty(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                File.Copy(sourceFilePath, destinationFilePath, true);

                // Re-open connection before logging to ensure DbContext is usable for the log entry
                await _context.Database.OpenConnectionAsync();

                await _activityLogService.LogActivityAsync(
                    userId: userId,
                    userName: userName,
                    activityType: "DatabaseBackup",
                    description: $"User '{userName}' created a database backup.",
                    targetEntityType: "System",
                    targetEntityDescription: Path.GetFileName(destinationFilePath)
                );

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error backing up database: {ex.Message}");
                // In a real app, use a proper logger
                return false;
            }
            finally
            {
                // Ensure the connection is re-opened if an error occurred after closing it
                if (_context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                {
                    await _context.Database.OpenConnectionAsync();
                }
            }
        }
    }
}
