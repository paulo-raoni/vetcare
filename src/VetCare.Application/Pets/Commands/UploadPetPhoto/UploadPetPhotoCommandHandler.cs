using MapsterMapper;
using MediatR;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Abstractions.Storage;
using VetCare.Application.Common.Exceptions;
using VetCare.Application.Pets.Specifications;
using VetCare.Domain.Pets;

namespace VetCare.Application.Pets.Commands.UploadPetPhoto;

public sealed class UploadPetPhotoCommandHandler : IRequestHandler<UploadPetPhotoCommand, PetDto>
{
    private readonly IRepository<Pet> _pets;
    private readonly IVetCareDbContext _db;
    private readonly IStorageService _storage;
    private readonly IMapper _mapper;

    public UploadPetPhotoCommandHandler(
        IRepository<Pet> pets,
        IVetCareDbContext db,
        IStorageService storage,
        IMapper mapper)
    {
        _pets = pets;
        _db = db;
        _storage = storage;
        _mapper = mapper;
    }

    public async Task<PetDto> Handle(UploadPetPhotoCommand request, CancellationToken cancellationToken)
    {
        var pet = await _pets.SingleOrDefaultAsync(new PetByIdSpec(request.PetId), cancellationToken)
            ?? throw new NotFoundException(nameof(Pet), request.PetId);

        var extension = Path.GetExtension(request.FileName);
        var key = $"pets/{pet.TenantId}/{pet.Id}/{Guid.NewGuid():N}{extension}";

        var url = await _storage.UploadAsync(key, request.Content, request.ContentType, cancellationToken);

        pet.UpdatePhoto(url);
        await _db.SaveChangesAsync(cancellationToken);

        return _mapper.Map<PetDto>(pet);
    }
}
