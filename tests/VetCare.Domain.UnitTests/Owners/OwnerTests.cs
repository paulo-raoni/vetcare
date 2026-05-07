using FluentAssertions;
using VetCare.Domain.Owners;

namespace VetCare.Domain.UnitTests.Owners;

public sealed class OwnerTests
{
    [Fact]
    public void Constructor_normalizes_email_and_raises_event()
    {
        var tenantId = Guid.NewGuid();
        var owner = new Owner(tenantId, "Jane Doe", "+5511999999999", "Jane@Example.COM");

        owner.TenantId.Should().Be(tenantId);
        owner.FullName.Should().Be("Jane Doe");
        owner.Email.Should().Be("jane@example.com");

        owner.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OwnerCreatedEvent>();
    }

    [Fact]
    public void Constructor_rejects_empty_full_name()
    {
        var act = () => new Owner(Guid.NewGuid(), "  ", "+1", "owner@example.com");
        act.Should().Throw<ArgumentException>().WithParameterName("fullName");
    }
}
