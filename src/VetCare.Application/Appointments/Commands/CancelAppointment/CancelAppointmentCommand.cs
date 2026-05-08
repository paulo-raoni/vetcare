using MediatR;

namespace VetCare.Application.Appointments.Commands.CancelAppointment;

public sealed record CancelAppointmentCommand(Guid Id) : IRequest<AppointmentDto>;
