using FluentAssertions;
using MapsterMapper;
using NSubstitute;
using VetCare.Application.Abstractions.MultiTenancy;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Abstractions.Specifications;
using VetCare.Application.Common.Exceptions;
using VetCare.Application.Pets;
using VetCare.Application.Pets.Commands.CreatePet;
using VetCare.Domain.Owners;
using VetCare.Domain.Pets;

namespace VetCare.Application.UnitTests.Pets;

public sealed class CreatePetHandlerTests
{
    private readonly IRepository<Pet> _pets = Substitute.For<IRepository<Pet>>();
    private readonly IRepository<Owner> _owners = Substitute.For<IRepository<Owner>>();
    private readonly IVetCareDbContext _db = Substitute.For<IVetCareDbContext>();
    private readonly ITenantProvider _tenant = Substitute.For<ITenantProvider>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();

    private readonly Guid _tenantId = Guid.NewGuid();

    public CreatePetHandlerTests()
    {
        _tenant.HasTenant.Returns(true);
        _tenant.TenantId.Returns(_tenantId);
        _mapper.Map<PetDto>(Arg.Any<Pet>())
            .Returns(ci =>
            {
                var p = ci.Arg<Pet>();
                return new PetDto(p.Id, p.TenantId, p.OwnerId, p.Name, p.Species, p.Breed, p.DateOfBirth, p.PhotoUrl, p.CreatedAt, p.UpdatedAt);
            });
    }

    [Fact]
    public async Task Handle_creates_pet_when_owner_exists()
    {
        var owner = new Owner(_tenantId, "Jane Doe", "+1", "jane@example.com");
        _owners.SingleOrDefaultAsync(Arg.Any<ISpecification<Owner>>(), Arg.Any<CancellationToken>())
            .Returns(owner);

        var handler = new CreatePetCommandHandler(_pets, _owners, _db, _tenant, _mapper);
        var command = new CreatePetCommand(owner.Id, "Rex", Species.Dog, "Labrador", new DateOnly(2022, 4, 15), null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Name.Should().Be("Rex");
        result.OwnerId.Should().Be(owner.Id);
        result.TenantId.Should().Be(_tenantId);
        _pets.Received(1).Add(Arg.Is<Pet>(p => p.Name == "Rex" && p.OwnerId == owner.Id));
        await _db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_NotFound_when_owner_missing()
    {
        _owners.SingleOrDefaultAsync(Arg.Any<ISpecification<Owner>>(), Arg.Any<CancellationToken>())
            .Returns((Owner?)null);

        var handler = new CreatePetCommandHandler(_pets, _owners, _db, _tenant, _mapper);
        var command = new CreatePetCommand(Guid.NewGuid(), "Rex", Species.Dog, "Labrador", new DateOnly(2022, 4, 15), null);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _pets.DidNotReceive().Add(Arg.Any<Pet>());
    }

    [Fact]
    public void Validator_rejects_future_birthdate()
    {
        var validator = new CreatePetCommandValidator();
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        var result = validator.Validate(new CreatePetCommand(Guid.NewGuid(), "Rex", Species.Dog, "Labrador", future, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePetCommand.DateOfBirth));
    }

    [Fact]
    public void Validator_rejects_blank_breed()
    {
        var validator = new CreatePetCommandValidator();

        var result = validator.Validate(new CreatePetCommand(Guid.NewGuid(), "Rex", Species.Dog, string.Empty, new DateOnly(2022, 1, 1), null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePetCommand.Breed));
    }

    [Fact]
    public void Validator_accepts_valid_command()
    {
        var validator = new CreatePetCommandValidator();

        var result = validator.Validate(new CreatePetCommand(Guid.NewGuid(), "Rex", Species.Dog, "Labrador", new DateOnly(2022, 1, 1), null));

        result.IsValid.Should().BeTrue();
    }
}
