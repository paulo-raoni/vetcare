using MapsterMapper;
using MediatR;
using VetCare.Application.Abstractions.MultiTenancy;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Common.Exceptions;
using VetCare.Application.Owners.Specifications;
using VetCare.Domain.Owners;
using VetCare.Domain.Pets;

namespace VetCare.Application.Pets.Commands.CreatePet;

public sealed class CreatePetCommandHandler : IRequestHandler<CreatePetCommand, PetDto>
{
    private readonly IRepository<Pet> _pets;
    private readonly IRepository<Owner> _owners;
    private readonly IVetCareDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly IMapper _mapper;

    public CreatePetCommandHandler(
        IRepository<Pet> pets,
        IRepository<Owner> owners,
        IVetCareDbContext db,
        ITenantProvider tenantProvider,
        IMapper mapper)
    {
        _pets = pets;
        _owners = owners;
        _db = db;
        _tenantProvider = tenantProvider;
        _mapper = mapper;
    }

    public async Task<PetDto> Handle(CreatePetCommand request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.HasTenant)
        {
            throw new InvalidOperationException("A tenant context is required to create a pet.");
        }

        var owner = await _owners.SingleOrDefaultAsync(new OwnerByIdSpec(request.OwnerId), cancellationToken)
            ?? throw new NotFoundException(nameof(Owner), request.OwnerId);

        var pet = new Pet(
            _tenantProvider.TenantId,
            owner.Id,
            request.Name,
            request.Species,
            request.Breed,
            request.DateOfBirth,
            request.PhotoUrl);

        _pets.Add(pet);
        await _db.SaveChangesAsync(cancellationToken);

        return _mapper.Map<PetDto>(pet);
    }
}
