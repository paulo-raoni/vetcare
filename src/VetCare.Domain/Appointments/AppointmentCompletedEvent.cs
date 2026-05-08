using VetCare.Domain.Primitives;

namespace VetCare.Domain.Appointments;

public sealed record AppointmentCompletedEvent(
    Guid AppointmentId,
    Guid TenantId,
    Guid PetId,
    Guid VetUserId,
    DateTime ScheduledAt) : IDomainEvent;
