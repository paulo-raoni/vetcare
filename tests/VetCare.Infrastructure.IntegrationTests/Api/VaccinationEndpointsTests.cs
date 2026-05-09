using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using VetCare.Domain.Pets;

namespace VetCare.Infrastructure.IntegrationTests.Api;

[Collection(IntegrationTestsCollection.Name)]
public sealed class VaccinationEndpointsTests
{
    private readonly VetCareWebApplicationFactory _factory;

    public VaccinationEndpointsTests(VetCareWebApplicationFactory factory)
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

    private sealed record VaccinationResponse(
        Guid Id,
        Guid TenantId,
        Guid PetId,
        string VaccineName,
        DateTime AdministeredAt,
        DateTime? NextDueAt,
        string BatchNumber,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    private sealed record PagedVaccinations(IReadOnlyList<VaccinationResponse> Items, int Page, int PageSize, int TotalCount, int TotalPages);

    [Fact]
    public async Task List_vaccinations_returns_200_with_recorded_entry()
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
        var pet = (await petResponse.Content.ReadFromJsonAsync<PetResponse>())!;

        var administered = DateTime.UtcNow.AddDays(-3);
        var recordResponse = await client.PostAsJsonAsync("/api/v1/vaccinations", new
        {
            petId = pet.Id,
            vaccineName = "Rabies",
            administeredAt = administered,
            nextDueAt = administered.AddYears(1),
            batchNumber = "RAB-2026-0412",
        });
        recordResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var listResponse = await client.GetAsync("/api/v1/vaccinations");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await listResponse.Content.ReadFromJsonAsync<PagedVaccinations>();
        page.Should().NotBeNull();
        page!.Items.Should().NotBeEmpty();
        page.TotalCount.Should().BeGreaterThanOrEqualTo(1);
        page.Items.Should().Contain(v => v.PetId == pet.Id && v.VaccineName == "Rabies");
    }
}
