using MapsterMapper;
using MediatR;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Common.Exceptions;
using VetCare.Domain.Appointments;

namespace VetCare.Application.Appointments.Queries.GetAppointmentById;

public sealed class GetAppointmentByIdQueryHandler : IRequestHandler<GetAppointmentByIdQuery, AppointmentDto>
{
    private readonly IRepository<Appointment> _appointments;
    private readonly IMapper _mapper;

    public GetAppointmentByIdQueryHandler(IRepository<Appointment> appointments, IMapper mapper)
    {
        _appointments = appointments;
        _mapper = mapper;
    }

    public async Task<AppointmentDto> Handle(GetAppointmentByIdQuery request, CancellationToken cancellationToken)
    {
        var appointment = await _appointments.GetByIdAsyncNoTracking(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), request.Id);

        return _mapper.Map<AppointmentDto>(appointment);
    }
}
