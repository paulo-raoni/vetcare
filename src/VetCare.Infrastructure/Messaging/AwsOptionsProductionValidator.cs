using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace VetCare.Infrastructure.Messaging;

internal sealed class AwsOptionsProductionValidator : IValidateOptions<AwsOptions>
{
    internal const string LocalStackCredentialValue = "test";

    internal const string AccessKeyMessage =
        "Aws:AccessKey 'test' is a LocalStack value and is not allowed outside Development.";

    internal const string SecretKeyMessage =
        "Aws:SecretKey 'test' is a LocalStack value and is not allowed outside Development.";

    internal const string ServiceUrlMessage =
        "Aws:ServiceUrl points to a local emulator and is not allowed outside Development.";

    private static readonly HashSet<string> LocalEmulatorHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "localhost",
        "127.0.0.1",
        "localstack",
    };

    private readonly IHostEnvironment _environment;

    public AwsOptionsProductionValidator(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, AwsOptions options)
    {
        if (_environment.IsDevelopment())
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();

        if (string.Equals(options.AccessKey, LocalStackCredentialValue, StringComparison.Ordinal))
        {
            failures.Add(AccessKeyMessage);
        }

        if (string.Equals(options.SecretKey, LocalStackCredentialValue, StringComparison.Ordinal))
        {
            failures.Add(SecretKeyMessage);
        }

        if (!string.IsNullOrWhiteSpace(options.ServiceUrl)
            && Uri.TryCreate(options.ServiceUrl, UriKind.Absolute, out var serviceUri)
            && LocalEmulatorHosts.Contains(serviceUri.Host))
        {
            failures.Add(ServiceUrlMessage);
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
