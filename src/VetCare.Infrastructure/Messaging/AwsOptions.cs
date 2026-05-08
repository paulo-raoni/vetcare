namespace VetCare.Infrastructure.Messaging;

public sealed class AwsOptions
{
    public const string SectionName = "Aws";

    public string Region { get; set; } = "us-east-1";

    public string? ServiceUrl { get; set; }

    public string? AccessKey { get; set; }

    public string? SecretKey { get; set; }
}
