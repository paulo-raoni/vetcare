using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using VetCare.Application.Abstractions.Storage;

namespace VetCare.Infrastructure.Storage;

public sealed class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly S3Options _options;

    public S3StorageService(IAmazonS3 s3, IOptions<S3Options> options)
    {
        _s3 = s3;
        _options = options.Value;
    }

    public async Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
        };

        await _s3.PutObjectAsync(request, ct);

        return BuildPublicUrl(key);
    }

    private string BuildPublicUrl(string key)
    {
        if (!string.IsNullOrWhiteSpace(_options.ServiceUrl))
        {
            return $"{_options.ServiceUrl.TrimEnd('/')}/{_options.BucketName}/{key}";
        }

        return $"https://{_options.BucketName}.s3.amazonaws.com/{key}";
    }
}
