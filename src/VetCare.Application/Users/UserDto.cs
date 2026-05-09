using VetCare.Domain.Users;

namespace VetCare.Application.Users;

public sealed record UserDto(
    Guid Id,
    Guid TenantId,
    string Email,
    UserRole Role,
    DateTime CreatedAt,
    DateTime UpdatedAt);
