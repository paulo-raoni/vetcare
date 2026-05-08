using MediatR;
using VetCare.Application.Abstractions.Messaging;

namespace VetCare.Application.Appointments.Commands.ScheduleAppointment;

public sealed record ScheduleAppointmentCommand(
    Guid PetId,
    Guid VetUserId,
    DateTime ScheduledAt,
    string? Notes) : IRequest<AppointmentDto>, ICommand;
