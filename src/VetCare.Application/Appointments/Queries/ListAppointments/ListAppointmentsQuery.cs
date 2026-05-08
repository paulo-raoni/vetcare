using MediatR;
using VetCare.Application.Common.Pagination;
using VetCare.Domain.Appointments;

namespace VetCare.Application.Appointments.Queries.ListAppointments;

public sealed record ListAppointmentsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? PetId = null,
    AppointmentStatus? Status = null,
    DateTime? From = null,
    DateTime? To = null) : IRequest<PagedResult<AppointmentDto>>;
