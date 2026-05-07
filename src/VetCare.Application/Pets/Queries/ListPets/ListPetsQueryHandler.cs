using MapsterMapper;
using MediatR;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Common.Pagination;
using VetCare.Application.Pets.Specifications;
using VetCare.Domain.Pets;

namespace VetCare.Application.Pets.Queries.ListPets;

public sealed class ListPetsQueryHandler : IRequestHandler<ListPetsQuery, PagedResult<PetDto>>
{
    private readonly IRepository<Pet> _pets;
    private readonly IMapper _mapper;

    public ListPetsQueryHandler(IRepository<Pet> pets, IMapper mapper)
    {
        _pets = pets;
        _mapper = mapper;
    }

    public async Task<PagedResult<PetDto>> Handle(ListPetsQuery request, CancellationToken cancellationToken)
    {
        var pageSpec = new PetListSpec(request.Page, request.PageSize, request.OwnerId);
        var countSpec = new PetListSpec(request.Page, request.PageSize, request.OwnerId, applyPaging: false);

        var items = await _pets.ListAsync(pageSpec, cancellationToken);
        var total = await _pets.CountAsync(countSpec, cancellationToken);

        var dtos = items.Select(p => _mapper.Map<PetDto>(p)).ToList();
        return new PagedResult<PetDto>(dtos, request.Page, request.PageSize, total);
    }
}
