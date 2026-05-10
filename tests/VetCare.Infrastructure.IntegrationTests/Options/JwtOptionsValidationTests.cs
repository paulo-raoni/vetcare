using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VetCare.Infrastructure.Identity;

namespace VetCare.Infrastructure.IntegrationTests.Options;

public sealed class JwtOptionsValidationTests
{
    private const string ValidIssuer = "vetcare-tests";
    private const string ValidAudience = "vetcare-tests";
    private const int ValidExpiryMinutes = 30;
    private const string ValidSecret = "A1b2C3d4-eF5gH6i_jK7lM8n.pQ9rS0t!uV%wX@yZ#kL$mN&hP*qR=oT+sB7w9zY";

    [Fact]
    public void Validation_rejects_placeholder_secret()
    {
        var act = () => ResolveJwtOptions(secret: "REPLACE_ME_WITH_A_LONG_RANDOM_SECRET_AT_LEAST_32_BYTES_LONG");

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*Jwt:Secret must not be the placeholder*");
    }

    [Fact]
    public void Validation_rejects_empty_secret_with_required_message()
    {
        var act = () => ResolveJwtOptions(secret: string.Empty);

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*Jwt:Secret is required.*");
    }

    [Fact]
    public void Validation_rejects_secret_shorter_than_32_bytes()
    {
        var act = () => ResolveJwtOptions(secret: "abcdefghijklmnop");

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*Jwt:Secret must be at least 32 bytes*");
    }

    [Fact]
    public void Validation_rejects_secret_with_low_entropy()
    {
        var act = () => ResolveJwtOptions(secret: new string('a', 40));

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*Jwt:Secret has too few distinct characters*");
    }

    [Fact]
    public void Validation_accepts_strong_secret()
    {
        var act = () => ResolveJwtOptions(secret: ValidSecret);

        act.Should().NotThrow();
    }

    private static JwtOptions ResolveJwtOptions(
        string secret,
        string issuer = ValidIssuer,
        string audience = ValidAudience,
        int expiryMinutes = ValidExpiryMinutes)
    {
        var services = new ServiceCollection();

        services.AddOptions<JwtOptions>()
            .Configure(o =>
            {
                o.Secret = secret;
                o.Issuer = issuer;
                o.Audience = audience;
                o.ExpiryMinutes = expiryMinutes;
            })
            .AddJwtOptionsValidation();

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<JwtOptions>>().Value;
    }
}
