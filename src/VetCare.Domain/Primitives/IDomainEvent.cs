using MediatR;

namespace VetCare.Domain.Primitives;

public interface IDomainEvent : INotification
{
    Guid EventId { get; }

    DateTime OccurredOnUtc { get; }
}
