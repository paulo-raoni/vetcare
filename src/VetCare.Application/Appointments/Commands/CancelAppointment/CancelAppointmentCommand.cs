using MediatR;
using VetCare.Application.Abstractions.Messaging;

namespace VetCare.Application.Appointments.Commands.CancelAppointment;

public sealed record CancelAppointmentCommand(Guid Id) : IRequest<AppointmentDto>, ICommand;
