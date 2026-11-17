using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using ReportChecker.Abstractions;
using ReportChecker.Exceptions;
using ReportChecker.Models;

namespace ReportChecker.Application.Services;

public class ReportShareService(
    IReportShareRepository shareRepository,
    IReportAccessService accessService,
    IUserRepository userRepository,
    ILogger<ReportShareService> logger) : IReportShareService
{
    public async Task<IReadOnlyList<ReportShareInfo>> GetSharesAsync(Guid reportId, Guid requesterId,
        CancellationToken ct = default)
    {
        await accessService.RequireOwnerAsync(reportId, requesterId, ct);
        var shares = await shareRepository.GetSharesAsync(reportId, ct);
        var infos = await Task.WhenAll(shares.Select(async share => new ReportShareInfo
        {
            Share = share,
            User = await GetUserSafeAsync(share.UserId, ct),
        }));
        return infos;
    }

    public async Task<ReportShareInfo> ShareWithUserAsync(Guid reportId, Guid requesterId, string loginOrEmail,
        ReportAccessLevel accessLevel, CancellationToken ct = default)
    {
        var access = await accessService.RequireOwnerAsync(reportId, requesterId, ct);
        ValidateLevel(accessLevel);

        if (string.IsNullOrWhiteSpace(loginOrEmail))
            throw new BadRequestException("Укажите email или логин пользователя");
        var query = loginOrEmail.Trim();

        var users = await userRepository.FindUsersByLoginOrEmailAsync(query, ct);
        if (users.Count == 0)
            throw new NotFoundException($"Пользователь '{query}' не найден");
        if (users.Count > 1)
            throw new BadRequestException(
                $"По запросу '{query}' найдено несколько пользователей, укажите точный email или логин");

        var user = users[0];
        if (user.Id == access.Report.OwnerId)
            throw new BadRequestException("Нельзя выдать доступ владельцу отчета");

        var existing = await shareRepository.GetShareAsync(reportId, user.Id, ct);
        if (existing != null)
            throw new BadRequestException("Пользователю уже выдан доступ к отчету");

        var share = await shareRepository.CreateShareAsync(reportId, user.Id, accessLevel, ct);
        return new ReportShareInfo
        {
            Share = share,
            User = user,
        };
    }

    public async Task<ReportShareInfo> UpdateShareAsync(Guid reportId, Guid requesterId, Guid userId,
        ReportAccessLevel accessLevel, CancellationToken ct = default)
    {
        await accessService.RequireOwnerAsync(reportId, requesterId, ct);
        ValidateLevel(accessLevel);

        var existing = await shareRepository.GetShareAsync(reportId, userId, ct);
        if (existing == null)
            throw new NotFoundException($"Доступ пользователя '{userId}' не найден");

        if (!await shareRepository.UpdateShareAsync(reportId, userId, accessLevel, ct))
            throw new NotFoundException($"Доступ пользователя '{userId}' не найден");

        var updated = await shareRepository.GetShareAsync(reportId, userId, ct) ?? new ReportShare
        {
            ReportId = existing.ReportId,
            UserId = existing.UserId,
            AccessLevel = accessLevel,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
        };
        return new ReportShareInfo
        {
            Share = updated,
            User = await GetUserSafeAsync(userId, ct),
        };
    }

    public async Task RevokeShareAsync(Guid reportId, Guid requesterId, Guid userId,
        CancellationToken ct = default)
    {
        await accessService.RequireOwnerAsync(reportId, requesterId, ct);
        if (!await shareRepository.DeleteShareAsync(reportId, userId, ct))
            throw new NotFoundException($"Доступ пользователя '{userId}' не найден");
    }

    public async Task<ReportShareLink?> GetLinkAsync(Guid reportId, Guid requesterId,
        CancellationToken ct = default)
    {
        await accessService.RequireOwnerAsync(reportId, requesterId, ct);
        return await shareRepository.GetLinkAsync(reportId, ct);
    }

    public async Task<ReportShareLink> CreateLinkAsync(Guid reportId, Guid requesterId,
        CancellationToken ct = default)
    {
        await accessService.RequireOwnerAsync(reportId, requesterId, ct);
        return await shareRepository.CreateOrGetLinkAsync(reportId, GenerateToken(), ct);
    }

    public async Task<ReportShareLink> RotateLinkAsync(Guid reportId, Guid requesterId,
        CancellationToken ct = default)
    {
        await accessService.RequireOwnerAsync(reportId, requesterId, ct);
        return await shareRepository.ReplaceLinkAsync(reportId, GenerateToken(), ct);
    }

    public async Task RevokeLinkAsync(Guid reportId, Guid requesterId, CancellationToken ct = default)
    {
        await accessService.RequireOwnerAsync(reportId, requesterId, ct);
        if (!await shareRepository.DeleteLinkAsync(reportId, ct))
            throw new NotFoundException("Публичная ссылка не найдена");
    }

    private static void ValidateLevel(ReportAccessLevel accessLevel)
    {
        if (accessLevel is not (ReportAccessLevel.Read or ReportAccessLevel.Comment or ReportAccessLevel.Edit))
            throw new BadRequestException("Недопустимый уровень доступа");
    }

    private async Task<UserInfo?> GetUserSafeAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            return await userRepository.GetUserByIdAsync(userId, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to load user {userId} for report share", userId);
            return null;
        }
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
