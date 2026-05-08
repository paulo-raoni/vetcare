using MapsterMapper;
using MediatR;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Common.Pagination;
using VetCare.Application.Vaccinations.Specifications;
using VetCare.Domain.Vaccinations;

namespace VetCare.Application.Vaccinations.Queries.ListVaccinations;

public sealed class ListVaccinationsQueryHandler : IRequestHandler<ListVaccinationsQuery, PagedResult<VaccinationDto>>
{
    private readonly IRepository<Vaccination> _vaccinations;
    private readonly IMapper _mapper;

    public ListVaccinationsQueryHandler(IRepository<Vaccination> vaccinations, IMapper mapper)
    {
        _vaccinations = vaccinations;
        _mapper = mapper;
    }

    public async Task<PagedResult<VaccinationDto>> Handle(ListVaccinationsQuery request, CancellationToken cancellationToken)
    {
        var pageSpec = new VaccinationListSpec(request.Page, request.PageSize, request.PetId);
        var countSpec = new VaccinationListSpec(request.Page, request.PageSize, request.PetId, applyPaging: false);

        var items = await _vaccinations.ListAsync(pageSpec, cancellationToken);
        var total = await _vaccinations.CountAsync(countSpec, cancellationToken);

        var dtos = items.Select(v => _mapper.Map<VaccinationDto>(v)).ToList();
        return new PagedResult<VaccinationDto>(dtos, request.Page, request.PageSize, total);
    }
}
