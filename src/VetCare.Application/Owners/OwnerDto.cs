namespace VetCare.Application.Owners;

public sealed record OwnerDto(
    Guid Id,
    Guid TenantId,
    string FullName,
    string Phone,
    string Email,
    DateTime CreatedAt,
    DateTime UpdatedAt);
