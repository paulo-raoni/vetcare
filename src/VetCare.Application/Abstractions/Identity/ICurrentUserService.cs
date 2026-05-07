using VetCare.Domain.Users;

namespace VetCare.Application.Abstractions.Identity;

public interface ICurrentUserService
{
    Guid? UserId { get; }

    Guid? TenantId { get; }

    UserRole? Role { get; }

    bool IsAuthenticated { get; }
}
