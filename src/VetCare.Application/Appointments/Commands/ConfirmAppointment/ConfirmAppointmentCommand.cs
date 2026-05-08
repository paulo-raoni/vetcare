using MediatR;
using VetCare.Application.Abstractions.Messaging;

namespace VetCare.Application.Appointments.Commands.ConfirmAppointment;

public sealed record ConfirmAppointmentCommand(Guid Id) : IRequest<AppointmentDto>, ICommand;
