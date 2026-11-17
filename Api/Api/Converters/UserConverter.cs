using ReportChecker.Api.Dtos;
using ReportChecker.Models;

namespace ReportChecker.Api.Converters;

public static class UserConverter
{
    public static PublicUserDto ToPublicDto(this UserInfo user)
    {
        var account = user.Accounts.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.Name)) ??
                      user.Accounts.FirstOrDefault();
        return new PublicUserDto
        {
            Username = account?.Name ?? account?.Login,
            AvatarUrl = account?.AvatarUrl,
        };
    }
}
