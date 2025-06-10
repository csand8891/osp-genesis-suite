using System;
using System.Threading.Tasks;
using RuleArchitect.Abstractions.DTOs;
using GenesisSentry.Entities;
using GenesisSentry.Interfaces;
using Microsoft.EntityFrameworkCore;
using RuleArchitect.Abstractions.Interfaces;

namespace GenesisSentry.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IAuthenticationDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUserActivityLogService _activityLogService;

        public AuthenticationService(IAuthenticationDbContext dbContext, IPasswordHasher passwordHasher, IUserActivityLogService activityLogService)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
            _activityLogService = activityLogService ?? throw new ArgumentNullException(nameof(activityLogService));
        }

        public async Task<AuthenticationResult> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return AuthenticationResult.Failure("Username and password are required.");
            }

            // 1. Find the active user by username
            var userEntity = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == username && u.IsActive);
            if (userEntity == null)
            {
                // User not found or active
                return AuthenticationResult.Failure("Invalid username or password");
            }

            // 2. Verify the provided password against the stored hash and salt
            bool isPasswordValid = _passwordHasher.VerifyPassword(password, userEntity.PasswordHash, userEntity.PasswordSalt);

            if (!isPasswordValid)
            {
                // Password does not match
                return AuthenticationResult.Failure("Invalid username or password");
            }

            // 3. Authentication successful: Map to DTO
            var userDto = new UserDto
            {
                UserId = userEntity.UserId,
                UserName = userEntity.UserName,
                Role = userEntity.Role,
                IsActive = userEntity.IsActive
            };

            // Update LastLoginDate
            userEntity.LastLoginDate = DateTime.UtcNow;
            await _activityLogService.LogActivityAsync(
                userId: userDto.UserId,
                userName: userDto.UserName,
                activityType: "UserLogin",
                description: $"User '{userDto.UserName}' logged in successfully.",
                saveChanges: false // Let the service handle the save
            );

            await _dbContext.SaveChangesAsync();

            return AuthenticationResult.Success(userDto);
        }

        /// <summary>
        /// Creates a new user and logs the creation event.
        /// NOTE: The IAuthenticationService interface must be updated to match this signature.
        /// </summary>
        /// <param name="username">The new user's username.</param>
        /// <param name="password">The new user's password.</param>
        /// <param name="role">The role assigned to the new user.</param>
        /// <param name="creatorUserId">The ID of the administrator creating the user.</param>
        /// <param name="creatorUsername">The username of the administrator creating the user.</param>
        /// <returns>A DTO for the newly created user.</returns>
        public async Task<UserDto> CreateUserAsync(string username, string password, string role, int creatorUserId, string creatorUsername)
        {
            if (await _dbContext.Users.AnyAsync(u => u.UserName == username))
            {
                throw new ArgumentException($"Username '{username}' is already taken.");
            }

            var (hash, salt) = _passwordHasher.HashPassword(password);
            var newUser = new UserEntity
            {
                UserName = username,
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = role,
                IsActive = true
            };
            _dbContext.Users.Add(newUser);

            // Log the creation activity. We will save it with the new user in one transaction.
            await _activityLogService.LogActivityAsync(
                userId: creatorUserId,
                userName: creatorUsername,
                activityType: "CreateUser",
                description: $"User '{creatorUsername}' created new user '{username}' with role '{role}'.",
                targetEntityType: "User",
                targetEntityDescription: newUser.UserName,
                saveChanges: false
            );

            await _dbContext.SaveChangesAsync();

            // After saving, newUser.UserId will be populated.
            // We could update the log with the new ID, but the description is usually sufficient.

            return new UserDto
            {
                UserId = newUser.UserId,
                UserName = newUser.UserName,
                Role = newUser.Role,
                IsActive = newUser.IsActive
            };
        }
    }
}
