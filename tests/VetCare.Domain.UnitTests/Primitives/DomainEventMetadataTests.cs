using System.Text.Json;
using FluentAssertions;
using VetCare.Domain.Appointments;
using VetCare.Domain.Owners;
using VetCare.Domain.Pets;
using VetCare.Domain.Primitives;
using VetCare.Domain.Vaccinations;

namespace VetCare.Domain.UnitTests.Primitives;

public class DomainEventMetadataTests
{
    private static readonly DateTime FixedScheduled =
        new(2026, 5, 15, 10, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime FixedAdministered =
        new(2026, 5, 1, 9, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void AppointmentScheduledEvent_metadata_is_stable_across_reads()
    {
        IDomainEvent sut = new AppointmentScheduledEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            FixedScheduled);

        AssertStable(sut);
    }

    [Fact]
    public void AppointmentCancelledEvent_metadata_is_stable_across_reads()
    {
        IDomainEvent sut = new AppointmentCancelledEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            FixedScheduled);

        AssertStable(sut);
    }

    [Fact]
    public void AppointmentCompletedEvent_metadata_is_stable_across_reads()
    {
        IDomainEvent sut = new AppointmentCompletedEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            FixedScheduled);

        AssertStable(sut);
    }

    [Fact]
    public void VaccinationRecordedEvent_metadata_is_stable_across_reads()
    {
        IDomainEvent sut = new VaccinationRecordedEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Rabies", FixedAdministered, null);

        AssertStable(sut);
    }

    [Fact]
    public void OwnerCreatedEvent_metadata_is_stable_across_reads()
    {
        IDomainEvent sut = new OwnerCreatedEvent(
            Guid.NewGuid(), Guid.NewGuid(), "Jane Doe");

        AssertStable(sut);
    }

    [Fact]
    public void PetRegisteredEvent_metadata_is_stable_across_reads()
    {
        IDomainEvent sut = new PetRegisteredEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Rex", Species.Dog);

        AssertStable(sut);
    }

    [Fact]
    public void AppointmentScheduledEvent_round_trips_through_json()
    {
        var original = new AppointmentScheduledEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            FixedScheduled);

        AssertRoundTrip(original);
    }

    [Fact]
    public void AppointmentCancelledEvent_round_trips_through_json()
    {
        var original = new AppointmentCancelledEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            FixedScheduled);

        AssertRoundTrip(original);
    }

    [Fact]
    public void AppointmentCompletedEvent_round_trips_through_json()
    {
        var original = new AppointmentCompletedEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            FixedScheduled);

        AssertRoundTrip(original);
    }

    [Fact]
    public void VaccinationRecordedEvent_round_trips_through_json()
    {
        var original = new VaccinationRecordedEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Rabies", FixedAdministered, FixedAdministered.AddYears(1));

        AssertRoundTrip(original);
    }

    [Fact]
    public void OwnerCreatedEvent_round_trips_through_json()
    {
        var original = new OwnerCreatedEvent(
            Guid.NewGuid(), Guid.NewGuid(), "Jane Doe");

        AssertRoundTrip(original);
    }

    [Fact]
    public void PetRegisteredEvent_round_trips_through_json()
    {
        var original = new PetRegisteredEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Rex", Species.Dog);

        AssertRoundTrip(original);
    }

    private static void AssertStable(IDomainEvent sut)
    {
        var firstId = sut.EventId;
        var firstAt = sut.OccurredOnUtc;

        sut.EventId.Should().Be(firstId);
        sut.OccurredOnUtc.Should().Be(firstAt);
    }

    private static void AssertRoundTrip<T>(T original) where T : IDomainEvent
    {
        var json = JsonSerializer.Serialize(original);
        var roundTripped = JsonSerializer.Deserialize<T>(json);

        roundTripped.Should().Be(original);
    }
}
