using FluentAssertions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using VetCare.Infrastructure.Messaging;

namespace VetCare.Infrastructure.IntegrationTests.Options;

public sealed class AwsOptionsProductionValidatorTests
{
    [Fact]
    public void Development_environment_accepts_localstack_credentials()
    {
        var validator = CreateValidator(Environments.Development);
        var options = new AwsOptions
        {
            Region = "us-east-1",
            ServiceUrl = "http://localhost:4566",
            AccessKey = "test",
            SecretKey = "test",
        };

        var result = validator.Validate(name: null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Development_environment_accepts_real_credentials()
    {
        var validator = CreateValidator(Environments.Development);
        var options = new AwsOptions
        {
            Region = "us-east-1",
            ServiceUrl = null,
            AccessKey = "AKIAREAL",
            SecretKey = "real-secret",
        };

        var result = validator.Validate(name: null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Production_rejects_localstack_access_key()
    {
        var validator = CreateValidator(Environments.Production);
        var options = new AwsOptions
        {
            Region = "us-east-1",
            ServiceUrl = null,
            AccessKey = "test",
            SecretKey = "real-secret",
        };

        var result = validator.Validate(name: null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("Aws:AccessKey 'test'");
    }

    [Fact]
    public void Production_rejects_localstack_secret_key()
    {
        var validator = CreateValidator(Environments.Production);
        var options = new AwsOptions
        {
            Region = "us-east-1",
            ServiceUrl = null,
            AccessKey = "AKIAREAL",
            SecretKey = "test",
        };

        var result = validator.Validate(name: null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("Aws:SecretKey 'test'");
    }

    [Theory]
    [InlineData("http://localhost:4566")]
    [InlineData("http://127.0.0.1:4566")]
    [InlineData("http://LocalStack:4566")]
    public void Production_rejects_local_emulator_service_url(string serviceUrl)
    {
        var validator = CreateValidator(Environments.Production);
        var options = new AwsOptions
        {
            Region = "us-east-1",
            ServiceUrl = serviceUrl,
            AccessKey = "AKIAREAL",
            SecretKey = "real-secret",
        };

        var result = validator.Validate(name: null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("Aws:ServiceUrl points to a local emulator");
    }

    [Fact]
    public void Production_aggregates_multiple_violations()
    {
        var validator = CreateValidator(Environments.Production);
        var options = new AwsOptions
        {
            Region = "us-east-1",
            ServiceUrl = "http://localhost:4566",
            AccessKey = "test",
            SecretKey = "test",
        };

        var result = validator.Validate(name: null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().HaveCount(3);
        result.Failures.Should().Contain(f => f.Contains("Aws:AccessKey 'test'"));
        result.Failures.Should().Contain(f => f.Contains("Aws:SecretKey 'test'"));
        result.Failures.Should().Contain(f => f.Contains("Aws:ServiceUrl points to a local emulator"));
    }

    [Fact]
    public void Production_accepts_clean_credentials_with_no_service_url()
    {
        var validator = CreateValidator(Environments.Production);
        var options = new AwsOptions
        {
            Region = "us-east-1",
            ServiceUrl = null,
            AccessKey = "AKIAREAL",
            SecretKey = "real-secret",
        };

        var result = validator.Validate(name: null, options);

        result.Succeeded.Should().BeTrue();
    }

    private static AwsOptionsProductionValidator CreateValidator(string environmentName)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        return new AwsOptionsProductionValidator(environment);
    }
}
