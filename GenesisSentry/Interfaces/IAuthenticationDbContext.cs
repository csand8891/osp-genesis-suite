using GenesisSentry.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
namespace GenesisSentry.Interfaces
{
    public interface IAuthenticationDbContext
    {
        DbSet<UserEntity> Users { get; } // Consuming DbContext will provide this DbSet
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}