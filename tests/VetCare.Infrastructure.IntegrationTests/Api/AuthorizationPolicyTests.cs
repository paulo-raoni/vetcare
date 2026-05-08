using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using VetCare.Domain.Pets;
using VetCare.Domain.Users;

namespace VetCare.Infrastructure.IntegrationTests.Api;

public sealed class AuthorizationPolicyTests : IClassFixture<VetCareWebApplicationFactory>
{
    private readonly VetCareWebApplicationFactory _factory;

    public AuthorizationPolicyTests(VetCareWebApplicationFactory factory)
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

    private async Task<(HttpClient AdminClient, AuthResponse Auth)> RegisterAdmin()
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

    private HttpClient ClientWithToken(string accessToken)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    [Fact]
    public async Task Receptionist_cannot_delete_owner_returns_403()
    {
        var (adminClient, adminAuth) = await RegisterAdmin();

        var ownerResponse = await adminClient.PostAsJsonAsync("/api/v1/owners", new
        {
            fullName = "Jane Doe",
            phone = "+5511999999999",
            email = $"jane-{Guid.NewGuid():N}@example.com",
        });
        ownerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var owner = (await ownerResponse.Content.ReadFromJsonAsync<OwnerResponse>())!;

        var (_, receptionistToken) = await _factory.CreateUserAndIssueTokenAsync(adminAuth.TenantId, UserRole.Receptionist);
        var receptionistClient = ClientWithToken(receptionistToken);

        var deleteResponse = await receptionistClient.DeleteAsync($"/api/v1/owners/{owner.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Receptionist_cannot_confirm_appointment_returns_403()
    {
        var (adminClient, adminAuth) = await RegisterAdmin();

        var ownerResponse = await adminClient.PostAsJsonAsync("/api/v1/owners", new
        {
            fullName = "Jane Doe",
            phone = "+5511999999999",
            email = $"jane-{Guid.NewGuid():N}@example.com",
        });
        var owner = (await ownerResponse.Content.ReadFromJsonAsync<OwnerResponse>())!;

        var petResponse = await adminClient.PostAsJsonAsync("/api/v1/pets", new
        {
            ownerId = owner.Id,
            name = "Rex",
            species = (int)Species.Dog,
            breed = "Labrador",
            dateOfBirth = "2022-04-15",
            photoUrl = (string?)null,
        });
        var pet = (await petResponse.Content.ReadFromJsonAsync<PetResponse>())!;

        var vetUserId = await _factory.CreateUserAsync(adminAuth.TenantId, UserRole.Vet);

        var scheduleResponse = await adminClient.PostAsJsonAsync("/api/v1/appointments", new
        {
            petId = pet.Id,
            vetUserId,
            scheduledAt = DateTime.UtcNow.AddDays(1),
            notes = (string?)null,
        });
        scheduleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var appointment = (await scheduleResponse.Content.ReadFromJsonAsync<AppointmentResponse>())!;

        var (_, receptionistToken) = await _factory.CreateUserAndIssueTokenAsync(adminAuth.TenantId, UserRole.Receptionist);
        var receptionistClient = ClientWithToken(receptionistToken);

        var confirmResponse = await receptionistClient.PutAsync($"/api/v1/appointments/{appointment.Id}/confirm", null);

        confirmResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
