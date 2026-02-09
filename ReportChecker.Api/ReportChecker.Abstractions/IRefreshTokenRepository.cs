namespace ReportChecker.Abstractions;

public interface IRefreshTokenRepository
{
    public Task CreateRefreshTokenAsync(Guid userId, string refreshToken);
    public Task<Guid?> GetUserIdByRefreshTokenAsync(string refreshToken);
    public Task UpdateRefreshTokenAsync(string oldRefreshToken, string newRefreshToken);
    public Task DeleteRefreshTokenAsync(string refreshToken);
}