using MapsterMapper;
using MediatR;
using VetCare.Application.Abstractions.MultiTenancy;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Common.Exceptions;
using VetCare.Application.Pets.Specifications;
using VetCare.Domain.Pets;
using VetCare.Domain.Vaccinations;

namespace VetCare.Application.Vaccinations.Commands.RecordVaccination;

public sealed class RecordVaccinationCommandHandler : IRequestHandler<RecordVaccinationCommand, VaccinationDto>
{
    private readonly IRepository<Vaccination> _vaccinations;
    private readonly IRepository<Pet> _pets;
    private readonly IVetCareDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly IMapper _mapper;

    public RecordVaccinationCommandHandler(
        IRepository<Vaccination> vaccinations,
        IRepository<Pet> pets,
        IVetCareDbContext db,
        ITenantProvider tenantProvider,
        IMapper mapper)
    {
        _vaccinations = vaccinations;
        _pets = pets;
        _db = db;
        _tenantProvider = tenantProvider;
        _mapper = mapper;
    }

    public async Task<VaccinationDto> Handle(RecordVaccinationCommand request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.HasTenant)
        {
            throw new InvalidOperationException("A tenant context is required to record a vaccination.");
        }

        var pet = await _pets.SingleOrDefaultAsync(new PetByIdSpec(request.PetId), cancellationToken)
            ?? throw new NotFoundException(nameof(Pet), request.PetId);

        var vaccination = new Vaccination(
            _tenantProvider.TenantId,
            pet.Id,
            request.VaccineName,
            request.AdministeredAt,
            request.NextDueAt,
            request.BatchNumber);

        _vaccinations.Add(vaccination);
        await _db.SaveChangesAsync(cancellationToken);

        return _mapper.Map<VaccinationDto>(vaccination);
    }
}
