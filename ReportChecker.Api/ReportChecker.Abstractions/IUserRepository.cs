namespace ReportChecker.Abstractions;

public interface IUserRepository
{
    public Task<Guid> CreateUserAsync();
    public Task DeleteUserAsync(Guid userId);
}