using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using VetCare.Domain.Pets;
using VetCare.Domain.Users;

namespace VetCare.Infrastructure.IntegrationTests.Api;

[Collection(IntegrationTestsCollection.Name)]
public sealed class FkViolationConflictTests
{
    private readonly VetCareWebApplicationFactory _factory;

    public FkViolationConflictTests(VetCareWebApplicationFactory factory)
    {
        _factory = factory;
    }

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

    private sealed record AppointmentResponse(
        Guid Id,
        Guid TenantId,
        Guid PetId,
        Guid VetUserId,
        DateTime ScheduledAt,
        int Status,
        string? Notes,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    private async Task<(HttpClient Client, AuthResponse Auth)> RegisterAdmin()
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
        var auth = (await register.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth);
    }

    [Fact]
    public async Task Delete_owner_with_pet_returns_409_problem_details()
    {
        var (client, _) = await RegisterAdmin();

        var ownerResponse = await client.PostAsJsonAsync("/api/v1/owners", new
        {
            fullName = "Jane Doe",
            phone = "+5511999999999",
            email = $"jane-{Guid.NewGuid():N}@example.com",
        });
        ownerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var owner = (await ownerResponse.Content.ReadFromJsonAsync<OwnerResponse>())!;

        var petResponse = await client.PostAsJsonAsync("/api/v1/pets", new
        {
            ownerId = owner.Id,
            name = "Rex",
            species = (int)Species.Dog,
            breed = "Labrador",
            dateOfBirth = "2022-04-15",
            photoUrl = (string?)null,
        });
        petResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var deleteResponse = await client.DeleteAsync($"/api/v1/owners/{owner.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        deleteResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var payload = await deleteResponse.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        root.GetProperty("status").GetInt32().Should().Be((int)HttpStatusCode.Conflict);
        root.GetProperty("title").GetString().Should().Contain("referenced");
        root.GetProperty("detail").GetString().Should().Be("The resource has dependent records.");
    }

    [Fact]
    public async Task Delete_pet_with_appointment_returns_409_problem_details()
    {
        var (client, auth) = await RegisterAdmin();

        var ownerResponse = await client.PostAsJsonAsync("/api/v1/owners", new
        {
            fullName = "John Roe",
            phone = "+5511988887777",
            email = $"john-{Guid.NewGuid():N}@example.com",
        });
        ownerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var owner = (await ownerResponse.Content.ReadFromJsonAsync<OwnerResponse>())!;

        var petResponse = await client.PostAsJsonAsync("/api/v1/pets", new
        {
            ownerId = owner.Id,
            name = "Bella",
            species = (int)Species.Cat,
            breed = "Siamese",
            dateOfBirth = "2021-09-01",
            photoUrl = (string?)null,
        });
        petResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var pet = (await petResponse.Content.ReadFromJsonAsync<PetResponse>())!;

        var vetUserId = await _factory.CreateUserAsync(auth.TenantId, UserRole.Vet);

        var scheduleResponse = await client.PostAsJsonAsync("/api/v1/appointments", new
        {
            petId = pet.Id,
            vetUserId,
            scheduledAt = DateTime.UtcNow.AddDays(1),
            notes = (string?)null,
        });
        scheduleResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var deleteResponse = await client.DeleteAsync($"/api/v1/pets/{pet.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        deleteResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var payload = await deleteResponse.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        root.GetProperty("status").GetInt32().Should().Be((int)HttpStatusCode.Conflict);
        root.GetProperty("title").GetString().Should().Contain("referenced");
        root.GetProperty("detail").GetString().Should().Be("The resource has dependent records.");
    }
}
