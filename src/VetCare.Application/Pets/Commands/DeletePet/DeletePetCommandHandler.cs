using MediatR;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Common.Exceptions;
using VetCare.Application.Pets.Specifications;
using VetCare.Domain.Pets;

namespace VetCare.Application.Pets.Commands.DeletePet;

public sealed class DeletePetCommandHandler : IRequestHandler<DeletePetCommand>
{
    private readonly IRepository<Pet> _pets;
    private readonly IVetCareDbContext _db;

    public DeletePetCommandHandler(IRepository<Pet> pets, IVetCareDbContext db)
    {
        _pets = pets;
        _db = db;
    }

    public async Task Handle(DeletePetCommand request, CancellationToken cancellationToken)
    {
        var pet = await _pets.SingleOrDefaultAsync(new PetByIdSpec(request.Id), cancellationToken)
            ?? throw new NotFoundException(nameof(Pet), request.Id);

        _pets.Remove(pet);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
