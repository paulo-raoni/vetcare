using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using VetCare.Domain.Pets;

namespace VetCare.Infrastructure.IntegrationTests.Api;

[Collection(IntegrationTestsCollection.Name)]
public sealed class PetEndpointsTests
{
    private readonly VetCareWebApplicationFactory _factory;

    public PetEndpointsTests(VetCareWebApplicationFactory factory)
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
        auth.Should().NotBeNull();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    private static async Task<OwnerResponse> CreateOwner(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/owners", new
        {
            fullName = "Jane Doe",
            phone = "+5511999999999",
            email = $"jane-{Guid.NewGuid():N}@example.com",
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var owner = await response.Content.ReadFromJsonAsync<OwnerResponse>();
        return owner!;
    }

    [Fact]
    public async Task List_pets_without_jwt_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/pets");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_pet_returns_201_with_payload()
    {
        var client = await AuthenticatedClient();
        var owner = await CreateOwner(client);

        var response = await client.PostAsJsonAsync("/api/v1/pets", new
        {
            ownerId = owner.Id,
            name = "Rex",
            species = (int)Species.Dog,
            breed = "Labrador",
            dateOfBirth = "2022-04-15",
            photoUrl = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var pet = await response.Content.ReadFromJsonAsync<PetResponse>();
        pet.Should().NotBeNull();
        pet!.Name.Should().Be("Rex");
        pet.OwnerId.Should().Be(owner.Id);
        pet.Species.Should().Be(Species.Dog);
        pet.Breed.Should().Be("Labrador");
        pet.DateOfBirth.Should().Be(new DateOnly(2022, 4, 15));
        response.Headers.Location!.ToString().Should().Contain($"/api/v1/pets/{pet.Id}");
    }

    [Fact]
    public async Task Get_pet_by_id_returns_200_with_data()
    {
        var client = await AuthenticatedClient();
        var owner = await CreateOwner(client);

        var createResponse = await client.PostAsJsonAsync("/api/v1/pets", new
        {
            ownerId = owner.Id,
            name = "Mittens",
            species = (int)Species.Cat,
            breed = "Siamese",
            dateOfBirth = "2021-09-01",
            photoUrl = (string?)null,
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<PetResponse>();
        created.Should().NotBeNull();

        var getResponse = await client.GetAsync($"/api/v1/pets/{created!.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<PetResponse>();
        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(created.Id);
        fetched.Name.Should().Be("Mittens");
        fetched.Species.Should().Be(Species.Cat);
        fetched.Breed.Should().Be("Siamese");
    }

    [Fact]
    public async Task Create_pet_with_unknown_owner_returns_404()
    {
        var client = await AuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/pets", new
        {
            ownerId = Guid.NewGuid(),
            name = "Rex",
            species = (int)Species.Dog,
            breed = "Labrador",
            dateOfBirth = "2022-04-15",
            photoUrl = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
