using FluentAssertions;
using FluentValidation;
using MapsterMapper;
using NSubstitute;
using VetCare.Application.Abstractions.MultiTenancy;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Owners;
using VetCare.Application.Owners.Commands.CreateOwner;
using VetCare.Domain.Owners;

namespace VetCare.Application.UnitTests.Owners;

public sealed class CreateOwnerHandlerTests
{
    private readonly IRepository<Owner> _repo = Substitute.For<IRepository<Owner>>();
    private readonly IVetCareDbContext _db = Substitute.For<IVetCareDbContext>();
    private readonly ITenantProvider _tenant = Substitute.For<ITenantProvider>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();

    private readonly Guid _tenantId = Guid.NewGuid();

    public CreateOwnerHandlerTests()
    {
        _tenant.HasTenant.Returns(true);
        _tenant.TenantId.Returns(_tenantId);
        _mapper.Map<OwnerDto>(Arg.Any<Owner>())
            .Returns(ci =>
            {
                var o = ci.Arg<Owner>();
                return new OwnerDto(o.Id, o.TenantId, o.FullName, o.Phone, o.Email, o.CreatedAt, o.UpdatedAt);
            });
    }

    [Fact]
    public async Task Handle_persists_owner_and_returns_dto()
    {
        var handler = new CreateOwnerCommandHandler(_repo, _db, _tenant, _mapper);
        var command = new CreateOwnerCommand("Jane Doe", "+5511999999999", "jane@example.com");

        var result = await handler.Handle(command, CancellationToken.None);

        result.FullName.Should().Be("Jane Doe");
        result.Email.Should().Be("jane@example.com");
        result.TenantId.Should().Be(_tenantId);
        _repo.Received(1).Add(Arg.Is<Owner>(o => o.FullName == "Jane Doe" && o.TenantId == _tenantId));
        await _db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_when_no_tenant_context()
    {
        var anonTenant = Substitute.For<ITenantProvider>();
        anonTenant.HasTenant.Returns(false);

        var handler = new CreateOwnerCommandHandler(_repo, _db, anonTenant, _mapper);
        var command = new CreateOwnerCommand("Jane Doe", "+1", "jane@example.com");

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _repo.DidNotReceive().Add(Arg.Any<Owner>());
    }

    [Fact]
    public void Validator_rejects_blank_email()
    {
        var validator = new CreateOwnerCommandValidator();

        var result = validator.Validate(new CreateOwnerCommand("Jane Doe", "+1", string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateOwnerCommand.Email));
    }

    [Fact]
    public void Validator_rejects_invalid_email_format()
    {
        var validator = new CreateOwnerCommandValidator();

        var result = validator.Validate(new CreateOwnerCommand("Jane Doe", "+1", "not-an-email"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateOwnerCommand.Email));
    }

    [Fact]
    public void Validator_accepts_valid_command()
    {
        var validator = new CreateOwnerCommandValidator();

        var result = validator.Validate(new CreateOwnerCommand("Jane Doe", "+5511999999999", "jane@example.com"));

        result.IsValid.Should().BeTrue();
    }
}
