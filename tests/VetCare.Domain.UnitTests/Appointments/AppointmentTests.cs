using FluentAssertions;
using VetCare.Domain.Appointments;
using VetCare.Domain.Primitives;

namespace VetCare.Domain.UnitTests.Appointments;

public sealed class AppointmentTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid PetId = Guid.NewGuid();
    private static readonly Guid VetUserId = Guid.NewGuid();

    [Fact]
    public void Constructor_creates_appointment_with_status_scheduled_and_event()
    {
        var when = DateTime.UtcNow.AddDays(1);

        var appointment = new Appointment(TenantId, PetId, VetUserId, when, "checkup");

        appointment.TenantId.Should().Be(TenantId);
        appointment.PetId.Should().Be(PetId);
        appointment.VetUserId.Should().Be(VetUserId);
        appointment.ScheduledAt.Should().Be(when);
        appointment.Status.Should().Be(AppointmentStatus.Scheduled);
        appointment.Notes.Should().Be("checkup");

        appointment.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AppointmentScheduledEvent>();
    }

    [Fact]
    public void Constructor_rejects_past_scheduled_at()
    {
        var act = () => new Appointment(TenantId, PetId, VetUserId, DateTime.UtcNow.AddMinutes(-1));
        act.Should().Throw<ArgumentException>().WithParameterName("scheduledAt");
    }

    [Fact]
    public void Constructor_rejects_notes_longer_than_max()
    {
        var notes = new string('x', Appointment.NotesMaxLength + 1);
        var act = () => new Appointment(TenantId, PetId, VetUserId, DateTime.UtcNow.AddDays(1), notes);
        act.Should().Throw<ArgumentException>().WithParameterName("notes");
    }

    [Fact]
    public void Confirm_transitions_scheduled_to_confirmed()
    {
        var appointment = new Appointment(TenantId, PetId, VetUserId, DateTime.UtcNow.AddDays(1));

        appointment.Confirm();

        appointment.Status.Should().Be(AppointmentStatus.Confirmed);
    }

    [Fact]
    public void Confirm_throws_when_already_cancelled()
    {
        var appointment = new Appointment(TenantId, PetId, VetUserId, DateTime.UtcNow.AddDays(1));
        appointment.Cancel();

        var act = () => appointment.Confirm();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_raises_cancelled_event()
    {
        var appointment = new Appointment(TenantId, PetId, VetUserId, DateTime.UtcNow.AddDays(1));
        appointment.ClearDomainEvents();

        appointment.Cancel();

        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
        appointment.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AppointmentCancelledEvent>();
    }

    [Fact]
    public void Complete_requires_confirmed_status()
    {
        var appointment = new Appointment(TenantId, PetId, VetUserId, DateTime.UtcNow.AddDays(1));

        var act = () => appointment.Complete();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Complete_after_confirm_raises_completed_event()
    {
        var appointment = new Appointment(TenantId, PetId, VetUserId, DateTime.UtcNow.AddDays(1));
        appointment.Confirm();
        appointment.ClearDomainEvents();

        appointment.Complete();

        appointment.Status.Should().Be(AppointmentStatus.Completed);
        appointment.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AppointmentCompletedEvent>();
    }
}
