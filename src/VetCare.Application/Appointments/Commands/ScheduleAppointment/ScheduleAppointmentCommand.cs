using MediatR;

namespace VetCare.Application.Appointments.Commands.ScheduleAppointment;

public sealed record ScheduleAppointmentCommand(
    Guid PetId,
    Guid VetUserId,
    DateTime ScheduledAt,
    string? Notes) : IRequest<AppointmentDto>;
