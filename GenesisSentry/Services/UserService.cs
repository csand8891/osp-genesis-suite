
using GenesisSentry.Entities;
using GenesisSentry.Interfaces;
using Microsoft.EntityFrameworkCore;
using RuleArchitect.Abstractions.DTOs;
using RuleArchitect.Abstractions.Interfaces; // Added for IUserActivityLogService
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GenesisSentry.Services
{
    public class UserService : IUserService
    {
        private readonly IAuthenticationDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUserActivityLogService _activityLogService; // INJECTED

        public UserService(
            IAuthenticationDbContext dbContext,
            IPasswordHasher passwordHasher,
            IUserActivityLogService activityLogService) // ADDED
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
            _activityLogService = activityLogService ?? throw new ArgumentNullException(nameof(activityLogService)); // STORED
        }

        // Method signature updated to match the interface
        public async Task<UserDto> CreateUserAsync(string username, string password, string role, bool isActive, int creatorUserId, string creatorUsername)
        {
            // 1. Validation
            if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username cannot be empty", nameof(username));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password cannot be empty", nameof(password));
            if (string.IsNullOrWhiteSpace(role)) throw new ArgumentException("Role cannot be empty", nameof(role));
            if (await _dbContext.Users.AnyAsync(u => u.UserName == username))
            {
                throw new ArgumentException($"Username '{username}' is already taken.");
            }

            // 2. Create the new User Entity
            var (hashedPassword, salt) = _passwordHasher.HashPassword(password);
            var newUserEntity = new UserEntity
            {
                UserName = username,
                PasswordHash = hashedPassword,
                PasswordSalt = salt,
                IsActive = isActive,
                Role = role
            };
            _dbContext.Users.Add(newUserEntity);

            // 3. Log the activity
            // We set saveChanges: false because we want to save the new user and the log entry together in one transaction.
            await _activityLogService.LogActivityAsync(
                userId: creatorUserId,
                userName: creatorUsername,
                activityType: "CreateUser",
                description: $"User '{creatorUsername}' created new user '{username}' with role '{role}'.",
                targetEntityType: "User",
                targetEntityDescription: newUserEntity.UserName,
                saveChanges: false
            );

            // 4. Save everything to the database
            await _dbContext.SaveChangesAsync();

            // At this point, newUserEntity.UserId is populated by the database.
            // We could retrieve the log entry we just added and update its TargetEntityId, but for now, this is sufficient.

            // 5. Return the DTO
            return new UserDto
            {
                UserId = newUserEntity.UserId,
                UserName = newUserEntity.UserName,
                Role = newUserEntity.Role,
                IsActive = newUserEntity.IsActive
            };
        }

        // --- Other methods from IUserService would go here ---

        public async Task<int> GetActiveUserCountAsync()
        {
            return await _dbContext.Users.CountAsync(u => u.IsActive);
        }

        public async Task<UserDto> GetUserByIdAsync(int userId)
        {
            var userEntity = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
            if (userEntity == null) return null;
            return new UserDto { UserId = userEntity.UserId, UserName = userEntity.UserName, Role = userEntity.Role, IsActive = userEntity.IsActive };
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            return await _dbContext.Users.AsNoTracking().Select(u => new UserDto
            {
                UserId = u.UserId,
                UserName = u.UserName,
                Role = u.Role,
                IsActive = u.IsActive
            }).ToListAsync();
        }

        public Task<UserDto> UpdateUserAsync(UpdateUserDto updateUserDto, int modifierUserId, string modifierUsername)
        {
            // TODO: Implement update logic, including logging the action.
            throw new NotImplementedException();
        }

        public Task<bool> DeleteUserAsync(int userId, int deleterUserId, string deleterUsername)
        {
            // TODO: Implement delete logic, including logging the action.
            throw new NotImplementedException();
        }
    }
}
