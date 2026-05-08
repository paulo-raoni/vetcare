using VetCare.Domain.Primitives;

namespace VetCare.Domain.Vaccinations;

public sealed record VaccinationRecordedEvent(
    Guid VaccinationId,
    Guid TenantId,
    Guid PetId,
    string VaccineName,
    DateTime AdministeredAt,
    DateTime? NextDueAt) : IDomainEvent;
