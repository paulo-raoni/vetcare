namespace VetCare.Infrastructure.Storage;

public sealed class S3Options
{
    public const string SectionName = "S3";

    public string BucketName { get; set; } = "vetcare";

    public string? ServiceUrl { get; set; }
}
