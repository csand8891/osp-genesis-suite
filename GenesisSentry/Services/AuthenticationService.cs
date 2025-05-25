using System;
using System.Threading.Tasks;
using GenesisSentry.DTOs;
using GenesisSentry.Entities;
using GenesisSentry.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GenesisSentry.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IAuthenticationDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        public AuthenticationService(IAuthenticationDbContext dbContext, IPasswordHasher passwordHasher)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
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
            await _dbContext.SaveChangesAsync();

            return AuthenticationResult.Success(userDto);
        }

        public async Task<UserDto> CreateUserAsync(string username, string password, string role)
        {
            // Check for existing user
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
            await _dbContext.SaveChangesAsync();
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
