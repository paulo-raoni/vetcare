using FluentAssertions;
using MapsterMapper;
using NSubstitute;
using VetCare.Application.Abstractions.MultiTenancy;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Abstractions.Specifications;
using VetCare.Application.Common.Exceptions;
using VetCare.Application.Vaccinations;
using VetCare.Application.Vaccinations.Commands.RecordVaccination;
using VetCare.Domain.Pets;
using VetCare.Domain.Vaccinations;

namespace VetCare.Application.UnitTests.Vaccinations;

public sealed class RecordVaccinationHandlerTests
{
    private readonly IRepository<Vaccination> _vaccinations = Substitute.For<IRepository<Vaccination>>();
    private readonly IRepository<Pet> _pets = Substitute.For<IRepository<Pet>>();
    private readonly IVetCareDbContext _db = Substitute.For<IVetCareDbContext>();
    private readonly ITenantProvider _tenant = Substitute.For<ITenantProvider>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly Guid _tenantId = Guid.NewGuid();

    public RecordVaccinationHandlerTests()
    {
        _tenant.HasTenant.Returns(true);
        _tenant.TenantId.Returns(_tenantId);
        _mapper.Map<VaccinationDto>(Arg.Any<Vaccination>())
            .Returns(ci =>
            {
                var v = ci.Arg<Vaccination>();
                return new VaccinationDto(v.Id, v.TenantId, v.PetId, v.VaccineName, v.AdministeredAt, v.NextDueAt, v.BatchNumber, v.CreatedAt, v.UpdatedAt);
            });
    }

    [Fact]
    public async Task Handle_records_vaccination_when_pet_exists()
    {
        var pet = new Pet(_tenantId, Guid.NewGuid(), "Rex", Species.Dog, "Lab", new DateOnly(2022, 1, 1));
        _pets.SingleOrDefaultAsync(Arg.Any<ISpecification<Pet>>(), Arg.Any<CancellationToken>())
            .Returns(pet);

        var handler = new RecordVaccinationCommandHandler(_vaccinations, _pets, _db, _tenant, _mapper);
        var administered = DateTime.UtcNow.AddDays(-5);

        var result = await handler.Handle(
            new RecordVaccinationCommand(pet.Id, "Rabies", administered, administered.AddYears(1), "RAB-001"),
            CancellationToken.None);

        result.PetId.Should().Be(pet.Id);
        result.VaccineName.Should().Be("Rabies");
        _vaccinations.Received(1).Add(Arg.Any<Vaccination>());
        await _db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_rejects_future_administered_at()
    {
        var validator = new RecordVaccinationCommandValidator();

        var result = validator.Validate(new RecordVaccinationCommand(
            Guid.NewGuid(), "Rabies", DateTime.UtcNow.AddDays(1), null, "RAB-001"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordVaccinationCommand.AdministeredAt));
    }

    [Fact]
    public void Validator_rejects_empty_vaccine_name()
    {
        var validator = new RecordVaccinationCommandValidator();

        var result = validator.Validate(new RecordVaccinationCommand(
            Guid.NewGuid(), string.Empty, DateTime.UtcNow.AddDays(-1), null, "RAB-001"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordVaccinationCommand.VaccineName));
    }

    [Fact]
    public async Task Handle_throws_NotFound_when_pet_missing()
    {
        _pets.SingleOrDefaultAsync(Arg.Any<ISpecification<Pet>>(), Arg.Any<CancellationToken>())
            .Returns((Pet?)null);

        var handler = new RecordVaccinationCommandHandler(_vaccinations, _pets, _db, _tenant, _mapper);

        var act = async () => await handler.Handle(
            new RecordVaccinationCommand(Guid.NewGuid(), "Rabies", DateTime.UtcNow.AddDays(-1), null, "RAB-001"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
