using MapsterMapper;
using MediatR;
using VetCare.Application.Abstractions.MultiTenancy;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Common.Exceptions;
using VetCare.Application.Pets.Specifications;
using VetCare.Application.Users.Specifications;
using VetCare.Domain.Appointments;
using VetCare.Domain.Pets;
using VetCare.Domain.Users;

namespace VetCare.Application.Appointments.Commands.ScheduleAppointment;

public sealed class ScheduleAppointmentCommandHandler : IRequestHandler<ScheduleAppointmentCommand, AppointmentDto>
{
    private readonly IRepository<Appointment> _appointments;
    private readonly IRepository<Pet> _pets;
    private readonly IRepository<User> _users;
    private readonly IVetCareDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly IMapper _mapper;

    public ScheduleAppointmentCommandHandler(
        IRepository<Appointment> appointments,
        IRepository<Pet> pets,
        IRepository<User> users,
        IVetCareDbContext db,
        ITenantProvider tenantProvider,
        IMapper mapper)
    {
        _appointments = appointments;
        _pets = pets;
        _users = users;
        _db = db;
        _tenantProvider = tenantProvider;
        _mapper = mapper;
    }

    public async Task<AppointmentDto> Handle(ScheduleAppointmentCommand request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.HasTenant)
        {
            throw new InvalidOperationException("A tenant context is required to schedule an appointment.");
        }

        var pet = await _pets.SingleOrDefaultAsync(new PetByIdSpec(request.PetId), cancellationToken)
            ?? throw new NotFoundException(nameof(Pet), request.PetId);

        var vet = await _users.SingleOrDefaultAsync(new UserByIdSpec(request.VetUserId), cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.VetUserId);

        if (vet.TenantId != _tenantProvider.TenantId || vet.Role != UserRole.Vet)
        {
            throw new NotFoundException(nameof(User), request.VetUserId);
        }

        var appointment = new Appointment(
            _tenantProvider.TenantId,
            pet.Id,
            request.VetUserId,
            request.ScheduledAt,
            request.Notes);

        _appointments.Add(appointment);
        await _db.SaveChangesAsync(cancellationToken);

        return _mapper.Map<AppointmentDto>(appointment);
    }
}
