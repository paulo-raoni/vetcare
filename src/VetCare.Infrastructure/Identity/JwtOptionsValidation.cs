using System.Text;
using Microsoft.Extensions.Options;

namespace VetCare.Infrastructure.Identity;

internal static class JwtOptionsValidation
{
    internal const string PlaceholderPrefix = "REPLACE_ME";
    internal const int MinSecretByteLength = 32;
    internal const int MinSecretDistinctCharacters = 16;

    internal static OptionsBuilder<JwtOptions> AddJwtOptionsValidation(this OptionsBuilder<JwtOptions> builder)
    {
        return builder
            .Validate(o => !string.IsNullOrWhiteSpace(o.Secret), "Jwt:Secret is required.")
            .Validate(
                o => !o.Secret.StartsWith(PlaceholderPrefix, StringComparison.OrdinalIgnoreCase),
                "Jwt:Secret must not be the placeholder. Provide a real secret via env vars (Jwt__Secret) or a non-versioned settings file.")
            .Validate(
                o => Encoding.UTF8.GetByteCount(o.Secret) >= MinSecretByteLength,
                "Jwt:Secret must be at least 32 bytes (HS256 minimum).")
            .Validate(
                o => o.Secret.Distinct().Count() >= MinSecretDistinctCharacters,
                "Jwt:Secret has too few distinct characters; supply a high-entropy random value.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "Jwt:Issuer is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "Jwt:Audience is required.")
            .Validate(o => o.ExpiryMinutes > 0, "Jwt:ExpiryMinutes must be > 0.");
    }
}
