using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;
using VetCare.Application.Behaviors;

namespace VetCare.Application.UnitTests.Behaviors;

public sealed class ValidationBehaviorTests
{
    public sealed record SampleRequest(string Name) : IRequest<string>;

    [Fact]
    public async Task Calls_next_when_no_validators_registered()
    {
        var behavior = new ValidationBehavior<SampleRequest, string>(Array.Empty<IValidator<SampleRequest>>());

        var result = await behavior.Handle(
            new SampleRequest("ok"),
            () => Task.FromResult("called"),
            CancellationToken.None);

        result.Should().Be("called");
    }

    [Fact]
    public async Task Calls_next_when_all_validators_pass()
    {
        var validator = Substitute.For<IValidator<SampleRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<SampleRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var behavior = new ValidationBehavior<SampleRequest, string>(new[] { validator });

        var result = await behavior.Handle(
            new SampleRequest("ok"),
            () => Task.FromResult("called"),
            CancellationToken.None);

        result.Should().Be("called");
    }

    [Fact]
    public async Task Throws_validation_exception_and_short_circuits_when_validator_fails()
    {
        var validator = Substitute.For<IValidator<SampleRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<SampleRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("Name", "Name is required") }));

        var behavior = new ValidationBehavior<SampleRequest, string>(new[] { validator });

        var nextCalled = false;
        Task<string> Next()
        {
            nextCalled = true;
            return Task.FromResult("called");
        }

        var act = async () => await behavior.Handle(new SampleRequest(string.Empty), Next, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        nextCalled.Should().BeFalse();
    }
}
