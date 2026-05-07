using VetCare.Domain.Primitives;

namespace VetCare.Domain.Pets;

public sealed record PetRegisteredEvent(Guid PetId, Guid TenantId, Guid OwnerId, string Name, Species Species) : IDomainEvent;
