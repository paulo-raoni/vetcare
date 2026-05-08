using MediatR;

namespace VetCare.Application.Appointments.Queries.GetAppointmentById;

public sealed record GetAppointmentByIdQuery(Guid Id) : IRequest<AppointmentDto>;
