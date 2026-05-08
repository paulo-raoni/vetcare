using MapsterMapper;
using MediatR;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Common.Exceptions;
using VetCare.Domain.Vaccinations;

namespace VetCare.Application.Vaccinations.Queries.GetVaccinationById;

public sealed class GetVaccinationByIdQueryHandler : IRequestHandler<GetVaccinationByIdQuery, VaccinationDto>
{
    private readonly IRepository<Vaccination> _vaccinations;
    private readonly IMapper _mapper;

    public GetVaccinationByIdQueryHandler(IRepository<Vaccination> vaccinations, IMapper mapper)
    {
        _vaccinations = vaccinations;
        _mapper = mapper;
    }

    public async Task<VaccinationDto> Handle(GetVaccinationByIdQuery request, CancellationToken cancellationToken)
    {
        var vaccination = await _vaccinations.GetByIdAsyncNoTracking(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Vaccination), request.Id);

        return _mapper.Map<VaccinationDto>(vaccination);
    }
}
