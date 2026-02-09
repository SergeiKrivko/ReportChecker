using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Repositories;

public class RefreshTokenRepository(ReportCheckerDbContext dbContext) : IRefreshTokenRepository
{
    public async Task CreateRefreshTokenAsync(Guid userId, string refreshToken)
    {
        await dbContext.AddAsync(new RefreshTokenEntity
        {
            Token = refreshToken,
            UserId = userId,
        });
    }

    public async Task<Guid?> GetUserIdByRefreshTokenAsync(string refreshToken)
    {
        return await dbContext.RefreshTokens
            .Where(x => x.Token == refreshToken)
            .Select(x => x.UserId)
            .FirstOrDefaultAsync();
    }

    public async Task UpdateRefreshTokenAsync(string oldRefreshToken, string newRefreshToken)
    {
        await dbContext.RefreshTokens
            .Where(x => x.Token == oldRefreshToken)
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.Token, newRefreshToken));
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteRefreshTokenAsync(string refreshToken)
    {
        await dbContext.RefreshTokens
            .Where(x => x.Token == refreshToken)
            .ExecuteDeleteAsync();
    }
}