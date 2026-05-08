using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using VetCare.Application.Abstractions.Auditing;
using VetCare.Application.Abstractions.Identity;
using VetCare.Application.Abstractions.Messaging;
using VetCare.Application.Auditing;
using VetCare.Application.Behaviors;
using VetCare.Domain.Users;

namespace VetCare.Application.UnitTests.Behaviors;

public sealed class AuditBehaviorTests
{
    public sealed record SampleCommand(string Name) : IRequest<string>, ICommand;

    public sealed record SampleQuery(Guid Id) : IRequest<string>;

    private readonly IAuditRepository _audit = Substitute.For<IAuditRepository>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();

    public AuditBehaviorTests()
    {
        _user.TenantId.Returns(Guid.NewGuid());
        _user.UserId.Returns(Guid.NewGuid());
        _user.Role.Returns(UserRole.Admin);
        _user.IsAuthenticated.Returns(true);
    }

    [Fact]
    public async Task Audits_command_after_handler_succeeds()
    {
        var behavior = new AuditBehavior<SampleCommand, string>(_audit, _user, NullLogger<AuditBehavior<SampleCommand, string>>.Instance);
        var command = new SampleCommand("rex");

        var result = await behavior.Handle(command, () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
        await _audit.Received(1).SaveAsync(
            Arg.Is<AuditEntry>(e =>
                e.Action == nameof(SampleCommand)
                && e.TenantId == _user.TenantId
                && e.UserId == _user.UserId
                && ReferenceEquals(e.Payload, command)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Does_not_audit_query()
    {
        var behavior = new AuditBehavior<SampleQuery, string>(_audit, _user, NullLogger<AuditBehavior<SampleQuery, string>>.Instance);

        var result = await behavior.Handle(new SampleQuery(Guid.NewGuid()), () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
        await _audit.DidNotReceive().SaveAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Does_not_audit_when_handler_throws()
    {
        var behavior = new AuditBehavior<SampleCommand, string>(_audit, _user, NullLogger<AuditBehavior<SampleCommand, string>>.Instance);

        var act = async () => await behavior.Handle(
            new SampleCommand("rex"),
            () => throw new InvalidOperationException("handler failed"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _audit.DidNotReceive().SaveAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>());
    }
}
