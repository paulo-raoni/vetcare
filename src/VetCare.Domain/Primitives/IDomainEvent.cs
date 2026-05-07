using MediatR;

namespace VetCare.Domain.Primitives;

public interface IDomainEvent : INotification
{
    Guid EventId => Guid.NewGuid();

    DateTime OccurredOn => DateTime.UtcNow;
}
