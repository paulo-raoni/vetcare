using MapsterMapper;
using MediatR;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Appointments.Specifications;
using VetCare.Application.Common.Pagination;
using VetCare.Domain.Appointments;

namespace VetCare.Application.Appointments.Queries.ListAppointments;

public sealed class ListAppointmentsQueryHandler : IRequestHandler<ListAppointmentsQuery, PagedResult<AppointmentDto>>
{
    private readonly IRepository<Appointment> _appointments;
    private readonly IMapper _mapper;

    public ListAppointmentsQueryHandler(IRepository<Appointment> appointments, IMapper mapper)
    {
        _appointments = appointments;
        _mapper = mapper;
    }

    public async Task<PagedResult<AppointmentDto>> Handle(ListAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var pageSpec = new AppointmentListSpec(request.Page, request.PageSize, request.PetId, request.Status, request.From, request.To);
        var countSpec = new AppointmentListSpec(request.Page, request.PageSize, request.PetId, request.Status, request.From, request.To, applyPaging: false);

        var items = await _appointments.ListAsync(pageSpec, cancellationToken);
        var total = await _appointments.CountAsync(countSpec, cancellationToken);

        var dtos = items.Select(a => _mapper.Map<AppointmentDto>(a)).ToList();
        return new PagedResult<AppointmentDto>(dtos, request.Page, request.PageSize, total);
    }
}
