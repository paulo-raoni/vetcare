using MediatR;

namespace VetCare.Application.Appointments.Commands.CompleteAppointment;

public sealed record CompleteAppointmentCommand(Guid Id) : IRequest<AppointmentDto>;
