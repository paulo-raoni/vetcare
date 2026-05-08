using MediatR;
using VetCare.Application.Abstractions.Messaging;

namespace VetCare.Application.Appointments.Commands.CompleteAppointment;

public sealed record CompleteAppointmentCommand(Guid Id) : IRequest<AppointmentDto>, ICommand;
