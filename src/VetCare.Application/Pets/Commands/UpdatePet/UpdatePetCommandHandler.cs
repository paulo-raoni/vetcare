using MapsterMapper;
using MediatR;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Common.Exceptions;
using VetCare.Application.Pets.Specifications;
using VetCare.Domain.Pets;

namespace VetCare.Application.Pets.Commands.UpdatePet;

public sealed class UpdatePetCommandHandler : IRequestHandler<UpdatePetCommand, PetDto>
{
    private readonly IRepository<Pet> _pets;
    private readonly IVetCareDbContext _db;
    private readonly IMapper _mapper;

    public UpdatePetCommandHandler(IRepository<Pet> pets, IVetCareDbContext db, IMapper mapper)
    {
        _pets = pets;
        _db = db;
        _mapper = mapper;
    }

    public async Task<PetDto> Handle(UpdatePetCommand request, CancellationToken cancellationToken)
    {
        var pet = await _pets.SingleOrDefaultAsync(new PetByIdSpec(request.Id), cancellationToken)
            ?? throw new NotFoundException(nameof(Pet), request.Id);

        pet.UpdateProfile(request.Name, request.Species, request.Breed, request.DateOfBirth, request.PhotoUrl);
        await _db.SaveChangesAsync(cancellationToken);

        return _mapper.Map<PetDto>(pet);
    }
}
