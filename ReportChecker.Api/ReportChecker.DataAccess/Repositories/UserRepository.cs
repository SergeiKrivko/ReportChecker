using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Repositories;

public class UserRepository(ReportCheckerDbContext dbContext) : IUserRepository
{
    public async Task<Guid> CreateUserAsync()
    {
        var id = Guid.NewGuid();
        var entity = new UserEntity
        {
            UserId = id,
            CreatedAt = DateTime.UtcNow,
            DeletedAt = null
        };
        await dbContext.Users.AddAsync(entity);
        await dbContext.SaveChangesAsync();
        return id;
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        await dbContext.Users
            .Where(e => e.UserId == userId && e.DeletedAt == null)
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.DeletedAt, DateTime.UtcNow));
        await dbContext.SaveChangesAsync();
    }
}