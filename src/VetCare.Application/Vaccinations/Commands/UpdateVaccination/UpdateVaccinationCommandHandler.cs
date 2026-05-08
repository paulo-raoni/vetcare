using MapsterMapper;
using MediatR;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Common.Exceptions;
using VetCare.Application.Vaccinations.Specifications;
using VetCare.Domain.Vaccinations;

namespace VetCare.Application.Vaccinations.Commands.UpdateVaccination;

public sealed class UpdateVaccinationCommandHandler : IRequestHandler<UpdateVaccinationCommand, VaccinationDto>
{
    private readonly IRepository<Vaccination> _vaccinations;
    private readonly IVetCareDbContext _db;
    private readonly IMapper _mapper;

    public UpdateVaccinationCommandHandler(
        IRepository<Vaccination> vaccinations,
        IVetCareDbContext db,
        IMapper mapper)
    {
        _vaccinations = vaccinations;
        _db = db;
        _mapper = mapper;
    }

    public async Task<VaccinationDto> Handle(UpdateVaccinationCommand request, CancellationToken cancellationToken)
    {
        var vaccination = await _vaccinations.SingleOrDefaultAsync(new VaccinationByIdSpec(request.Id), cancellationToken)
            ?? throw new NotFoundException(nameof(Vaccination), request.Id);

        vaccination.Update(request.VaccineName, request.AdministeredAt, request.NextDueAt, request.BatchNumber);
        await _db.SaveChangesAsync(cancellationToken);

        return _mapper.Map<VaccinationDto>(vaccination);
    }
}
