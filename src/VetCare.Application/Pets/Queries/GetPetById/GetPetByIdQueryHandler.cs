using MapsterMapper;
using MediatR;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Common.Exceptions;
using VetCare.Domain.Pets;

namespace VetCare.Application.Pets.Queries.GetPetById;

public sealed class GetPetByIdQueryHandler : IRequestHandler<GetPetByIdQuery, PetDto>
{
    private readonly IRepository<Pet> _pets;
    private readonly IMapper _mapper;

    public GetPetByIdQueryHandler(IRepository<Pet> pets, IMapper mapper)
    {
        _pets = pets;
        _mapper = mapper;
    }

    public async Task<PetDto> Handle(GetPetByIdQuery request, CancellationToken cancellationToken)
    {
        var pet = await _pets.GetByIdAsyncNoTracking(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Pet), request.Id);

        return _mapper.Map<PetDto>(pet);
    }
}
