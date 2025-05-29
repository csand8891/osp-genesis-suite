using GenesisSentry.DTOs; // If you return UserDto
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenesisSentry.Interfaces
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
        Task<UserDto> CreateUserAsync(string username, string password, string role, bool isActive);
        Task<UserDto> GetUserByIdAsync(int userId);
        Task<UserDto> UpdateUserAsync(UpdateUserDto updateUserDto);
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<bool> DeleteUserAsync(int userId);
    }
}