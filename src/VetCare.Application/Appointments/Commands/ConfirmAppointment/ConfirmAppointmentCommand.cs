using MediatR;

namespace VetCare.Application.Appointments.Commands.ConfirmAppointment;

public sealed record ConfirmAppointmentCommand(Guid Id) : IRequest<AppointmentDto>;
