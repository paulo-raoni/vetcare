using FluentAssertions;
using MapsterMapper;
using NSubstitute;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Abstractions.Specifications;
using VetCare.Application.Abstractions.Storage;
using VetCare.Application.Common.Exceptions;
using VetCare.Application.Pets;
using VetCare.Application.Pets.Commands.UploadPetPhoto;
using VetCare.Domain.Pets;

namespace VetCare.Application.UnitTests.Pets;

public sealed class UploadPetPhotoHandlerTests
{
    private readonly IRepository<Pet> _pets = Substitute.For<IRepository<Pet>>();
    private readonly IVetCareDbContext _db = Substitute.For<IVetCareDbContext>();
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();

    public UploadPetPhotoHandlerTests()
    {
        _mapper.Map<PetDto>(Arg.Any<Pet>())
            .Returns(ci =>
            {
                var p = ci.Arg<Pet>();
                return new PetDto(p.Id, p.TenantId, p.OwnerId, p.Name, p.Species, p.Breed, p.DateOfBirth, p.PhotoUrl, p.CreatedAt, p.UpdatedAt);
            });
    }

    [Fact]
    public async Task Handle_uploads_to_storage_and_updates_pet_photo_url()
    {
        var pet = new Pet(Guid.NewGuid(), Guid.NewGuid(), "Rex", Species.Dog, "Labrador", new DateOnly(2022, 4, 15));
        _pets.SingleOrDefaultAsync(Arg.Any<ISpecification<Pet>>(), Arg.Any<CancellationToken>())
            .Returns(pet);

        const string uploadedUrl = "http://localstack:4566/vetcare-pets/pets/x/y.jpg";
        _storage.UploadAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(uploadedUrl);

        var bytes = new byte[] { 0xFF, 0xD8, 0xFF };
        await using var content = new MemoryStream(bytes);
        var handler = new UploadPetPhotoCommandHandler(_pets, _db, _storage, _mapper);
        var command = new UploadPetPhotoCommand(pet.Id, "rex.jpg", content, "image/jpeg");

        var result = await handler.Handle(command, CancellationToken.None);

        await _storage.Received(1).UploadAsync(
            Arg.Is<string>(k => k.StartsWith($"pets/{pet.TenantId}/{pet.Id}/", StringComparison.Ordinal) && k.EndsWith(".jpg", StringComparison.Ordinal)),
            content,
            "image/jpeg",
            Arg.Any<CancellationToken>());
        await _db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        pet.PhotoUrl.Should().Be(uploadedUrl);
        result.PhotoUrl.Should().Be(uploadedUrl);
    }

    [Fact]
    public async Task Handle_throws_NotFound_when_pet_missing()
    {
        _pets.SingleOrDefaultAsync(Arg.Any<ISpecification<Pet>>(), Arg.Any<CancellationToken>())
            .Returns((Pet?)null);

        await using var content = new MemoryStream(new byte[] { 1, 2, 3 });
        var handler = new UploadPetPhotoCommandHandler(_pets, _db, _storage, _mapper);
        var command = new UploadPetPhotoCommand(Guid.NewGuid(), "rex.jpg", content, "image/jpeg");

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _storage.DidNotReceive().UploadAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
