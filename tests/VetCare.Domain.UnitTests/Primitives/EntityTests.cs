using FluentAssertions;
using VetCare.Domain.Primitives;

namespace VetCare.Domain.UnitTests.Primitives;

public sealed class EntityTests
{
    private sealed class TestEntity : Entity
    {
        public TestEntity()
        {
        }

        public TestEntity(Guid id)
            : base(id)
        {
        }
    }

    [Fact]
    public void Constructor_assigns_new_id_and_timestamps()
    {
        var before = DateTime.UtcNow;
        var entity = new TestEntity();
        var after = DateTime.UtcNow;

        entity.Id.Should().NotBeEmpty();
        entity.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        entity.UpdatedAt.Should().Be(entity.CreatedAt);
    }

    [Fact]
    public void Constructor_with_empty_id_throws()
    {
        var act = () => new TestEntity(Guid.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_is_by_id_and_type()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new TestEntity(id);
        var c = new TestEntity();

        a.Should().Be(b);
        a.Should().NotBe(c);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}
