using VetCare.Domain.Primitives;

namespace VetCare.Domain.Appointments;

public sealed record AppointmentCompletedEvent(
    Guid AppointmentId,
    Guid TenantId,
    Guid PetId,
    Guid VetUserId,
    DateTime ScheduledAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}
