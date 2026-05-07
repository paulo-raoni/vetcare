using VetCare.Domain.Users;

namespace VetCare.Application.Abstractions.Identity;

public interface IJwtTokenService
{
    AccessToken Generate(User user);
}

public sealed record AccessToken(string Token, DateTime ExpiresAt);
