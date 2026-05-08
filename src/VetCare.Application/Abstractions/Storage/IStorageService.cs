namespace VetCare.Application.Abstractions.Storage;

public interface IStorageService
{
    Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct);
}
