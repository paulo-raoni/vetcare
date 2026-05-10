using FluentAssertions;
using VetCare.Domain.Primitives;

namespace VetCare.Domain.UnitTests.Primitives;

public sealed class AggregateRootTests
{
    private sealed class TestAggregate : AggregateRoot
    {
    }

    private sealed record TestEvent(string Name) : IDomainEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();

        public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
    }

    [Fact]
    public void AddDomainEvent_appends_to_collection()
    {
        var aggregate = new TestAggregate();
        var evt = new TestEvent("foo");

        aggregate.AddDomainEvent(evt);

        aggregate.DomainEvents.Should().ContainSingle().Which.Should().Be(evt);
    }

    [Fact]
    public void AddDomainEvent_null_throws()
    {
        var aggregate = new TestAggregate();
        var act = () => aggregate.AddDomainEvent(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ClearDomainEvents_empties_collection()
    {
        var aggregate = new TestAggregate();
        aggregate.AddDomainEvent(new TestEvent("a"));
        aggregate.AddDomainEvent(new TestEvent("b"));

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.Should().BeEmpty();
    }
}
