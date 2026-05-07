using MapsterMapper;
using MediatR;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Common.Exceptions;
using VetCare.Application.Owners.Specifications;
using VetCare.Domain.Owners;

namespace VetCare.Application.Owners.Queries.GetOwnerById;

public sealed class GetOwnerByIdQueryHandler : IRequestHandler<GetOwnerByIdQuery, OwnerDto>
{
    private readonly IRepository<Owner> _owners;
    private readonly IMapper _mapper;

    public GetOwnerByIdQueryHandler(IRepository<Owner> owners, IMapper mapper)
    {
        _owners = owners;
        _mapper = mapper;
    }

    public async Task<OwnerDto> Handle(GetOwnerByIdQuery request, CancellationToken cancellationToken)
    {
        var owner = await _owners.SingleOrDefaultAsync(new OwnerByIdSpec(request.Id), cancellationToken)
            ?? throw new NotFoundException(nameof(Owner), request.Id);

        return _mapper.Map<OwnerDto>(owner);
    }
}
