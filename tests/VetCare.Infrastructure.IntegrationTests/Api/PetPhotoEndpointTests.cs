using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using NSubstitute;
using VetCare.Domain.Pets;

namespace VetCare.Infrastructure.IntegrationTests.Api;

[Collection(IntegrationTestsCollection.Name)]
public sealed class PetPhotoEndpointTests : IAsyncLifetime
{
    private readonly VetCareWebApplicationFactory _factory;

    public PetPhotoEndpointTests(VetCareWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync()
    {
        _factory.ClearSubstitutes();
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private sealed record AuthResponse(string AccessToken, DateTime ExpiresAt, Guid TenantId, Guid UserId, string Email, string Role);

    private sealed record OwnerResponse(Guid Id, Guid TenantId, string FullName, string Phone, string Email, DateTime CreatedAt, DateTime UpdatedAt);

    private sealed record PetResponse(
        Guid Id,
        Guid TenantId,
        Guid OwnerId,
        string Name,
        Species Species,
        string Breed,
        DateOnly DateOfBirth,
        string? PhotoUrl,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    private async Task<HttpClient> AuthenticatedClient()
    {
        var client = _factory.CreateClient();
        var slug = $"clinic-{Guid.NewGuid():N}";
        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantName = "Acme Vet",
            tenantSlug = slug,
            email = $"admin-{Guid.NewGuid():N}@acme.test",
            password = "ChangeMe123!",
        });
        register.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    private static async Task<PetResponse> CreatePet(HttpClient client)
    {
        var ownerResponse = await client.PostAsJsonAsync("/api/v1/owners", new
        {
            fullName = "Jane Doe",
            phone = "+5511999999999",
            email = $"jane-{Guid.NewGuid():N}@example.com",
        });
        ownerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var owner = await ownerResponse.Content.ReadFromJsonAsync<OwnerResponse>();

        var petResponse = await client.PostAsJsonAsync("/api/v1/pets", new
        {
            ownerId = owner!.Id,
            name = "Rex",
            species = (int)Species.Dog,
            breed = "Labrador",
            dateOfBirth = "2022-04-15",
            photoUrl = (string?)null,
        });
        petResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await petResponse.Content.ReadFromJsonAsync<PetResponse>())!;
    }

    [Fact]
    public async Task Upload_pet_photo_with_valid_jpeg_returns_200_and_photo_url()
    {
        const string uploadedUrl = "http://localhost:4566/vetcare-pets-tests/pets/x/y.jpg";
        _factory.StorageService.UploadAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(uploadedUrl);

        var client = await AuthenticatedClient();
        var pet = await CreatePet(client);

        using var content = new MultipartFormDataContent();
        var fileBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "rex.jpg");

        var response = await client.PostAsync($"/api/v1/pets/{pet.Id}/photo", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<PetResponse>();
        updated.Should().NotBeNull();
        updated!.PhotoUrl.Should().Be(uploadedUrl);

        await _factory.StorageService.Received(1).UploadAsync(
            Arg.Any<string>(),
            Arg.Any<Stream>(),
            "image/jpeg",
            Arg.Any<CancellationToken>());

        var auditCount = await _factory.CountAuditEntriesByActionAndEntityAsync("UploadPetPhotoCommand", pet.Id);
        auditCount.Should().Be(1);
    }

    [Fact]
    public async Task Upload_pet_photo_with_oversized_file_returns_400()
    {
        var client = await AuthenticatedClient();
        var pet = await CreatePet(client);

        using var content = new MultipartFormDataContent();
        var fileBytes = new byte[(5 * 1024 * 1024) + 1];
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "huge.jpg");

        var response = await client.PostAsync($"/api/v1/pets/{pet.Id}/photo", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_pet_photo_with_disallowed_content_type_returns_400()
    {
        var client = await AuthenticatedClient();
        var pet = await CreatePet(client);

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "rex.pdf");

        var response = await client.PostAsync($"/api/v1/pets/{pet.Id}/photo", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_pet_photo_with_valid_png_magic_returns_200()
    {
        const string uploadedUrl = "http://localhost:4566/vetcare-pets-tests/pets/x/y.png";
        _factory.StorageService.UploadAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(uploadedUrl);

        var client = await AuthenticatedClient();
        var pet = await CreatePet(client);

        using var content = new MultipartFormDataContent();
        var fileBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "rex.png");

        var response = await client.PostAsync($"/api/v1/pets/{pet.Id}/photo", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<PetResponse>();
        updated!.PhotoUrl.Should().Be(uploadedUrl);
    }

    [Fact]
    public async Task Upload_pet_photo_with_pdf_bytes_but_image_jpeg_content_type_returns_400()
    {
        var client = await AuthenticatedClient();
        var pet = await CreatePet(client);

        using var content = new MultipartFormDataContent();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "spoof.jpg");

        var response = await client.PostAsync($"/api/v1/pets/{pet.Id}/photo", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
