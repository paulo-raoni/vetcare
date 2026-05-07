using VetCare.Domain.Primitives;

namespace VetCare.Domain.Owners;

public sealed record OwnerCreatedEvent(Guid OwnerId, Guid TenantId, string FullName) : IDomainEvent;
