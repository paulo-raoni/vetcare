using NSubstitute;
using VetCare.Application.Abstractions.Messaging;
using VetCare.Application.Appointments.Events;
using VetCare.Domain.Appointments;

namespace VetCare.Application.UnitTests.Appointments;

public sealed class OnAppointmentScheduledTests
{
    [Fact]
    public async Task Publishes_reminder_message_to_appointment_reminders_queue()
    {
        var publisher = Substitute.For<ISqsPublisher>();
        var handler = new OnAppointmentScheduled(publisher);

        var notification = new AppointmentScheduledEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1));

        await handler.Handle(notification, CancellationToken.None);

        await publisher.Received(1).PublishAsync(
            QueueNames.AppointmentReminders,
            Arg.Any<AppointmentReminderMessage>(),
            Arg.Any<CancellationToken>());
    }
}
