using MediatR;
using VetCare.Application.Abstractions.Messaging;
using VetCare.Domain.Appointments;

namespace VetCare.Application.Appointments.Events;

public sealed class OnAppointmentScheduled : INotificationHandler<AppointmentScheduledEvent>
{
    private readonly ISqsPublisher _publisher;

    public OnAppointmentScheduled(ISqsPublisher publisher)
    {
        _publisher = publisher;
    }

    public Task Handle(AppointmentScheduledEvent notification, CancellationToken cancellationToken)
    {
        var message = new AppointmentReminderMessage(
            notification.AppointmentId,
            notification.TenantId,
            notification.PetId,
            notification.VetUserId,
            notification.ScheduledAt,
            "scheduled");

        return _publisher.PublishAsync(QueueNames.AppointmentReminders, message, cancellationToken);
    }
}
