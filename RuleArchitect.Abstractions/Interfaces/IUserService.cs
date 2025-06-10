using RuleArchitect.Abstractions.DTOs.Auth; // If you return UserDto
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RuleArchitect.Abstractions.Interfaces
{
    public interface IUserService
    {
        Task<int> GetActiveUserCountAsync();
        // Potentially other methods later:
        // Task<UserDto> GetUserByIdAsync(int userId);
        // Task<IEnumerable<UserDto>> GetAllUsersAsync();
        // Task<UserDto> CreateUserAsync(string username, string password, string role, bool isActive); // Consider if this duplicates AuthenticationService or centralizes it here
        // Task UpdateUserAsync(UserDto user);
        // Task DeleteUserAsync(int userId);
        Task<UserDto> CreateUserAsync(string username, string password, string role, bool isActive, int creatorUserId, string creatorUsername);
        Task<UserDto> GetUserByIdAsync(int userId);
        Task<UserDto> UpdateUserAsync(UpdateUserDto updateUserDto, int modifierUserId, string modifierUsername); // Recommended update for consistency
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<bool> DeleteUserAsync(int userId, int deleterUserId, string deleterUsername);
    }
}