using MapsterMapper;
using MediatR;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Appointments.Specifications;
using VetCare.Application.Common.Exceptions;
using VetCare.Domain.Appointments;

namespace VetCare.Application.Appointments.Commands.ConfirmAppointment;

public sealed class ConfirmAppointmentCommandHandler : IRequestHandler<ConfirmAppointmentCommand, AppointmentDto>
{
    private readonly IRepository<Appointment> _appointments;
    private readonly IVetCareDbContext _db;
    private readonly IMapper _mapper;

    public ConfirmAppointmentCommandHandler(
        IRepository<Appointment> appointments,
        IVetCareDbContext db,
        IMapper mapper)
    {
        _appointments = appointments;
        _db = db;
        _mapper = mapper;
    }

    public async Task<AppointmentDto> Handle(ConfirmAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointments.SingleOrDefaultAsync(new AppointmentByIdSpec(request.Id), cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), request.Id);

        appointment.Confirm();
        await _db.SaveChangesAsync(cancellationToken);

        return _mapper.Map<AppointmentDto>(appointment);
    }
}
