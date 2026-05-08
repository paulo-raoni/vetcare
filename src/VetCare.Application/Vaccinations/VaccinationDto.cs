namespace VetCare.Application.Vaccinations;

public sealed record VaccinationDto(
    Guid Id,
    Guid TenantId,
    Guid PetId,
    string VaccineName,
    DateTime AdministeredAt,
    DateTime? NextDueAt,
    string BatchNumber,
    DateTime CreatedAt,
    DateTime UpdatedAt);
