using FluentAssertions;
using MapsterMapper;
using NSubstitute;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Abstractions.Specifications;
using VetCare.Application.Vaccinations;
using VetCare.Application.Vaccinations.Queries.ListVaccinations;
using VetCare.Domain.Vaccinations;

namespace VetCare.Application.UnitTests.Vaccinations;

public sealed class ListVaccinationsHandlerTests
{
    private readonly IRepository<Vaccination> _vaccinations = Substitute.For<IRepository<Vaccination>>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly Guid _tenantId = Guid.NewGuid();

    public ListVaccinationsHandlerTests()
    {
        _mapper.Map<VaccinationDto>(Arg.Any<Vaccination>())
            .Returns(ci =>
            {
                var v = ci.Arg<Vaccination>();
                return new VaccinationDto(v.Id, v.TenantId, v.PetId, v.VaccineName, v.AdministeredAt, v.NextDueAt, v.BatchNumber, v.CreatedAt, v.UpdatedAt);
            });
    }

    [Fact]
    public async Task Handle_returns_paged_results_when_repository_has_items()
    {
        var petId = Guid.NewGuid();
        var administered = DateTime.UtcNow.AddDays(-7);
        var rabies = new Vaccination(_tenantId, petId, "Rabies", administered, administered.AddYears(1), "RAB-001");
        var dhpp = new Vaccination(_tenantId, petId, "DHPP", administered.AddDays(1), null, "DHPP-002");

        _vaccinations.ListAsync(Arg.Any<ISpecification<Vaccination>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { rabies, dhpp });
        _vaccinations.CountAsync(Arg.Any<ISpecification<Vaccination>>(), Arg.Any<CancellationToken>())
            .Returns(5);

        var handler = new ListVaccinationsQueryHandler(_vaccinations, _mapper);

        var result = await handler.Handle(new ListVaccinationsQuery(Page: 1, PageSize: 2, PetId: petId), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.VaccineName).Should().BeEquivalentTo(new[] { "Rabies", "DHPP" });
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task Handle_returns_empty_page_when_repository_has_no_items()
    {
        _vaccinations.ListAsync(Arg.Any<ISpecification<Vaccination>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Vaccination>());
        _vaccinations.CountAsync(Arg.Any<ISpecification<Vaccination>>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var handler = new ListVaccinationsQueryHandler(_vaccinations, _mapper);

        var result = await handler.Handle(new ListVaccinationsQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }
}
