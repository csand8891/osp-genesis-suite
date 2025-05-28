using GenesisSentry.Interfaces;
using GenesisSentry.Entities; // For UserEntity
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GenesisSentry.Services
{
    public class UserService : IUserService
    {
        private readonly IAuthenticationDbContext _dbContext;

        public UserService(IAuthenticationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<int> GetActiveUserCountAsync()
        {
            return await _dbContext.Users.CountAsync(u => u.IsActive);
        }

        // Implement other methods from IUserService here...
    }
}