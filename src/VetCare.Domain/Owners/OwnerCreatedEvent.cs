using VetCare.Domain.Primitives;

namespace VetCare.Domain.Owners;

public sealed record OwnerCreatedEvent(Guid OwnerId, Guid TenantId, string FullName) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}
