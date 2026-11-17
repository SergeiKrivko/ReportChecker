using ReportChecker.Api.Dtos;
using ReportChecker.Models;

namespace ReportChecker.Api.Converters;

public static class ReportShareConverter
{
    public static ReportShareDto ToDto(this ReportShareInfo info)
    {
        var account = info.User?.Accounts.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.Name)) ??
                      info.User?.Accounts.FirstOrDefault();
        return new ReportShareDto
        {
            UserId = info.Share.UserId,
            AccessLevel = info.Share.AccessLevel,
            CreatedAt = info.Share.CreatedAt,
            UpdatedAt = info.Share.UpdatedAt,
            Name = account?.Name,
            Login = account?.Login,
            Email = account?.Email,
            AvatarUrl = account?.AvatarUrl,
        };
    }

    public static ReportShareLinkDto ToDto(this ReportShareLink link)
    {
        return new ReportShareLinkDto
        {
            Token = link.Token,
            CreatedAt = link.CreatedAt,
        };
    }
}
