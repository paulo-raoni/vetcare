using MapsterMapper;
using MediatR;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Common.Pagination;
using VetCare.Application.Owners.Specifications;
using VetCare.Domain.Owners;

namespace VetCare.Application.Owners.Queries.ListOwners;

public sealed class ListOwnersQueryHandler : IRequestHandler<ListOwnersQuery, PagedResult<OwnerDto>>
{
    private readonly IRepository<Owner> _owners;
    private readonly IMapper _mapper;

    public ListOwnersQueryHandler(IRepository<Owner> owners, IMapper mapper)
    {
        _owners = owners;
        _mapper = mapper;
    }

    public async Task<PagedResult<OwnerDto>> Handle(ListOwnersQuery request, CancellationToken cancellationToken)
    {
        var pageSpec = new OwnerListSpec(request.Page, request.PageSize);
        var countSpec = new OwnerListSpec(request.Page, request.PageSize, applyPaging: false);

        var items = await _owners.ListAsync(pageSpec, cancellationToken);
        var total = await _owners.CountAsync(countSpec, cancellationToken);

        var dtos = items.Select(o => _mapper.Map<OwnerDto>(o)).ToList();
        return new PagedResult<OwnerDto>(dtos, request.Page, request.PageSize, total);
    }
}
