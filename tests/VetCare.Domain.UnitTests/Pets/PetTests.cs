using FluentAssertions;
using VetCare.Domain.Pets;

namespace VetCare.Domain.UnitTests.Pets;

public sealed class PetTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly DateOnly Dob = new(2020, 1, 1);

    [Fact]
    public void Constructor_sets_properties_and_raises_event()
    {
        var pet = new Pet(TenantId, OwnerId, "Rex", Species.Dog, "Labrador", Dob);

        pet.TenantId.Should().Be(TenantId);
        pet.OwnerId.Should().Be(OwnerId);
        pet.Name.Should().Be("Rex");
        pet.Species.Should().Be(Species.Dog);
        pet.Breed.Should().Be("Labrador");
        pet.DateOfBirth.Should().Be(Dob);

        pet.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PetRegisteredEvent>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_rejects_empty_name(string name)
    {
        var act = () => new Pet(TenantId, OwnerId, name, Species.Dog, "Labrador", Dob);
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void Constructor_rejects_empty_owner_id()
    {
        var act = () => new Pet(TenantId, Guid.Empty, "Rex", Species.Dog, "Labrador", Dob);
        act.Should().Throw<ArgumentException>().WithParameterName("ownerId");
    }

    [Fact]
    public void Constructor_rejects_empty_tenant_id()
    {
        var act = () => new Pet(Guid.Empty, OwnerId, "Rex", Species.Dog, "Labrador", Dob);
        act.Should().Throw<ArgumentException>().WithParameterName("tenantId");
    }

    [Fact]
    public void Constructor_rejects_future_birthdate()
    {
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
        var act = () => new Pet(TenantId, OwnerId, "Rex", Species.Dog, "Labrador", future);
        act.Should().Throw<ArgumentException>().WithParameterName("dateOfBirth");
    }
}
