using FluentAssertions;
using VetCare.Domain.Vaccinations;

namespace VetCare.Domain.UnitTests.Vaccinations;

public sealed class VaccinationTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid PetId = Guid.NewGuid();

    [Fact]
    public void Constructor_records_vaccination_and_raises_event()
    {
        var administered = DateTime.UtcNow.AddDays(-1);

        var vaccination = new Vaccination(TenantId, PetId, "Rabies", administered, administered.AddYears(1), "RAB-001");

        vaccination.TenantId.Should().Be(TenantId);
        vaccination.PetId.Should().Be(PetId);
        vaccination.VaccineName.Should().Be("Rabies");
        vaccination.AdministeredAt.Should().Be(administered);
        vaccination.NextDueAt.Should().Be(administered.AddYears(1));
        vaccination.BatchNumber.Should().Be("RAB-001");

        vaccination.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<VaccinationRecordedEvent>();
    }

    [Fact]
    public void Constructor_rejects_future_administered_at()
    {
        var act = () => new Vaccination(TenantId, PetId, "Rabies", DateTime.UtcNow.AddDays(1), null, "RAB-001");

        act.Should().Throw<ArgumentException>().WithParameterName("administeredAt");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_rejects_empty_vaccine_name(string name)
    {
        var act = () => new Vaccination(TenantId, PetId, name, DateTime.UtcNow.AddDays(-1), null, "RAB-001");

        act.Should().Throw<ArgumentException>().WithParameterName("vaccineName");
    }

    [Fact]
    public void Constructor_rejects_next_due_before_administered()
    {
        var administered = DateTime.UtcNow.AddDays(-1);
        var act = () => new Vaccination(TenantId, PetId, "Rabies", administered, administered.AddDays(-1), "RAB-001");

        act.Should().Throw<ArgumentException>().WithParameterName("nextDueAt");
    }
}
