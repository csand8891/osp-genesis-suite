using RuleArchitect.Abstractions.Interfaces;
using GenesisSentry.Interfaces;
using GenesisSentry.Entities; // For UserEntity
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using RuleArchitect.Abstractions.DTOs;
using System.Collections.Generic;

namespace GenesisSentry.Services
{
    public class UserService : IUserService
    {
        private readonly IAuthenticationDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        public UserService(IAuthenticationDbContext dbContext, IPasswordHasher passwordHasher)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(_passwordHasher));
        }

        public async Task<int> GetActiveUserCountAsync()
        {
            return await _dbContext.Users.CountAsync(u => u.IsActive);
        }

        public async Task<UserDto> CreateUserAsync(string username, string password, string role, bool isActive)
        {
            // Data validation
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentNullException("Username cannot be empty", nameof(username));
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentNullException("Password cannot be empty", nameof(password));
            }
            if (string.IsNullOrWhiteSpace(role))
            {
                throw new ArgumentNullException("Role cannot be empty", nameof(role));
            }

            // Check if existing user exists
            if (await _dbContext.Users.AnyAsync(u => u.UserName == username))
            {
                throw new ArgumentNullException("$Username '{ username }' is already taken.");

            }

            

            // Hash the password
            var (hashedPassword, salt) = _passwordHasher.HashPassword(password);

            // Create new User Entity
            var newUserEntity = new UserEntity
            {
                UserName = username,
                PasswordHash = hashedPassword,
                PasswordSalt = salt,
                IsActive = isActive,
                Role = role
            };
            //  Add user to DBContext
            try
            {
                _dbContext.Users.Add(newUserEntity);
                await _dbContext.SaveChangesAsync();
            } catch (DbUpdateException ex)
            {
                throw new Exception("An error occurred while creating user in the database", ex);

            }

            // Map user to DTO and return

            return new UserDto { 
                UserId = newUserEntity.UserId,
                UserName = newUserEntity.UserName,
                Role = newUserEntity.Role,
                IsActive = newUserEntity.IsActive
                
            
            };

        }

        public async Task<UserDto> UpdateUserAsync(UpdateUserDto updateUserDto)
        {
            var ExistingUserEntity = await _dbContext.Users.FindAsync(updateUserDto.UserId);
            if (ExistingUserEntity == null)
            {
                throw new KeyNotFoundException($"User with ID {updateUserDto.UserId} not found.");
            }

            bool hasChanges = false;

            if (!string.IsNullOrWhiteSpace(updateUserDto.UserName) && ExistingUserEntity.UserName != updateUserDto.UserName)
            {
                if (await _dbContext.Users.AnyAsync(u => u.UserId != updateUserDto.UserId && u.UserName.ToLower() == updateUserDto.UserName.ToLower())) { /* throw exception */ }
                ExistingUserEntity.UserName = updateUserDto.UserName;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(updateUserDto.UserName) && ExistingUserEntity.Role != updateUserDto.Role)
            {
                ExistingUserEntity.Role = updateUserDto.Role;
                hasChanges = true;
            }

            if (updateUserDto.IsActive.HasValue && ExistingUserEntity.IsActive != updateUserDto.IsActive)
            {
                ExistingUserEntity.IsActive = updateUserDto.IsActive.Value;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(updateUserDto.Password))
            {
                var (hashedPassword, salt) = _passwordHasher.HashPassword(updateUserDto.Password);
                ExistingUserEntity.PasswordHash = hashedPassword;
                ExistingUserEntity.PasswordSalt = salt;
            }

            if (!hasChanges)
            {
                return new UserDto
                {
                    UserId = ExistingUserEntity.UserId,
                    UserName = ExistingUserEntity.UserName,
                    Role = ExistingUserEntity.Role,
                    IsActive = ExistingUserEntity.IsActive
                };
            }

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new Exception("The user data has been modified by another process.  Please reload and try again", ex);
            }
            catch (DbUpdateException ex)
            {
                throw new Exception("An error occurred while saving user updates to the database.", ex);
            }
            return new UserDto
            {
                UserId = ExistingUserEntity.UserId,
                UserName = ExistingUserEntity.UserName,
                Role = ExistingUserEntity.Role,
                IsActive = ExistingUserEntity.IsActive
            };

            

        }

        public async Task<UserDto> GetUserByIdAsync(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId), "UserId must be positive.");
            }
            var existingUserEntity = await _dbContext.Users.FindAsync(userId);
            if (existingUserEntity == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            return new UserDto
            {
                UserId = existingUserEntity.UserId,
                UserName = existingUserEntity.UserName,
                Role = existingUserEntity.Role,
                IsActive = existingUserEntity.IsActive
            };
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var userEntities = await _dbContext.Users.AsNoTracking().ToListAsync();

            if (userEntities == null || !userEntities.Any())
            {
                return new List<UserDto>();
            }

            var userDtos = userEntities.Select(entity => new UserDto
            {
                UserId = entity.UserId,
                UserName = entity.UserName,
                Role = entity.Role,
                IsActive = entity.IsActive
            } );

            return userDtos;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            // a. Input Validation
            if (userId <= 0)
            {
                // Or throw new ArgumentOutOfRangeException(nameof(userId), "UserId must be positive.");
                // Depending on how you want to handle this, returning false might be acceptable
                // if the ViewModel layer is expected to validate IDs before calling.
                // For service layer robustness, throwing is often better.
                // For simplicity here, we'll let it proceed and it will likely result in 'user not found'.
            }

            // b. Fetch the User Entity
            var userToDelete = await _dbContext.Users.FindAsync(userId); // FindAsync is efficient for PK lookups

            // c. Check if User Exists
            if (userToDelete == null)
            {
                // User not found, so cannot delete. Return false.
                // Optionally, log this attempt or notify if it's considered an anomaly.
                return false;
            }

            // d. (Optional) Business Logic Checks
            // For example, you might prevent deletion of certain users:
            // if (userToDelete.UserName.ToLower() == "admin" && /* some logic to check if it's the last admin */)
            // {
            //     // Log this attempt
            //     // _notificationService.ShowError("Cannot delete the primary administrator account.");
            //     return false;
            // }
            // You might also need to consider what happens to data owned by this user
            // (e.g., Orders created by them). EF Core's foreign key constraints will
            // dictate this unless you handle it explicitly (e.g., cascade delete, set FKs to null, or prevent deletion).
            // For UserEntity, if 'Orders.CreatedByUserId' is a non-nullable FK to UserEntity.UserId,
            // and OnDelete is set to Restrict, deleting a user who has created orders will fail at SaveChangesAsync.

            // e. Remove from DbContext and Save
            try
            {
                _dbContext.Users.Remove(userToDelete);
                int changes = await _dbContext.SaveChangesAsync();
                return changes > 0; // Returns true if one or more records were affected (i.e., the user was deleted)
            }
            catch (DbUpdateException ex) // Catches issues like FK constraint violations
            {
                // Log the exception (ex)
                // This could happen if, for example, the user is referenced by other entities
                // and the database constraints prevent deletion (e.g., an Order has this user as CreatedByUserId
                // and the FK is set to RESTRICT on delete).
                // You might want to provide a more user-friendly error message or handle specific FK issues.
                // For now, we just indicate failure.
                // Consider: _notificationService.ShowError("Failed to delete user due to database constraints.", ex.Message);
                return false;
            }
            catch (Exception ex) // Catch any other unexpected errors
            {
                // Log the exception (ex)
                // Consider: _notificationService.ShowError("An unexpected error occurred while deleting the user.", ex.Message);
                return false;
            }
        }

    }
}
