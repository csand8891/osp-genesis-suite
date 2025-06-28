using System.Threading.Tasks;

namespace RuleArchitect.Abstractions.Interfaces
{
    /// <summary>
    /// Defines a contract for database management operations.
    /// </summary>
    public interface IDatabaseService
    {
        /// <summary>
        /// Creates a backup of the current application database.
        /// </summary>
        /// <param name="destinationFilePath">The full path where the backup file will be saved.</param>
        /// <param name="userId">The ID of the user performing the backup.</param>
        /// <param name="userName">The username of the user performing the backup.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result is true if the backup was successful; otherwise, false.</returns>
        Task<bool> BackupDatabaseAsync(string destinationFilePath, int userId, string userName);
    }
}