using MediatR;
using VetCare.Application.Abstractions.Messaging;
using VetCare.Domain.Appointments;

namespace VetCare.Application.Appointments.Events;

public sealed class OnAppointmentCancelled : INotificationHandler<AppointmentCancelledEvent>
{
    private readonly ISqsPublisher _publisher;

    public OnAppointmentCancelled(ISqsPublisher publisher)
    {
        _publisher = publisher;
    }

    public Task Handle(AppointmentCancelledEvent notification, CancellationToken cancellationToken)
    {
        var message = new AppointmentReminderMessage(
            notification.AppointmentId,
            notification.TenantId,
            notification.PetId,
            notification.VetUserId,
            notification.ScheduledAt,
            "cancelled");

        return _publisher.PublishAsync(QueueNames.AppointmentCancellations, message, cancellationToken);
    }
}
