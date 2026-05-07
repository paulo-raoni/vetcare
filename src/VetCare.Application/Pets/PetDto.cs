using VetCare.Domain.Pets;

namespace VetCare.Application.Pets;

public sealed record PetDto(
    Guid Id,
    Guid TenantId,
    Guid OwnerId,
    string Name,
    Species Species,
    string Breed,
    DateOnly DateOfBirth,
    string? PhotoUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt);
