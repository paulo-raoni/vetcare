using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace VetCare.Infrastructure.IntegrationTests.Api;

[Collection(IntegrationTestsCollection.Name)]
public sealed class OwnerEndpointsTests
{
    private readonly VetCareWebApplicationFactory _factory;

    public OwnerEndpointsTests(VetCareWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private sealed record AuthResponse(string AccessToken, DateTime ExpiresAt, Guid TenantId, Guid UserId, string Email, string Role);

    private sealed record OwnerResponse(Guid Id, Guid TenantId, string FullName, string Phone, string Email, DateTime CreatedAt, DateTime UpdatedAt);

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

    [Fact]
    public async Task List_owners_without_jwt_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/owners");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_owner_returns_201_with_payload()
    {
        var client = await AuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/owners", new
        {
            fullName = "Jane Doe",
            phone = "+5511999999999",
            email = "jane@example.com",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var owner = await response.Content.ReadFromJsonAsync<OwnerResponse>();
        owner.Should().NotBeNull();
        owner!.FullName.Should().Be("Jane Doe");
        owner.Email.Should().Be("jane@example.com");
        owner.TenantId.Should().NotBeEmpty();
        response.Headers.Location!.ToString().Should().Contain($"/api/v1/owners/{owner.Id}");
    }

    [Fact]
    public async Task Get_owner_by_id_returns_200_with_data()
    {
        var client = await AuthenticatedClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/owners", new
        {
            fullName = "John Roe",
            phone = "+5511988887777",
            email = "john@example.com",
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<OwnerResponse>();
        created.Should().NotBeNull();

        var getResponse = await client.GetAsync($"/api/v1/owners/{created!.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<OwnerResponse>();
        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(created.Id);
        fetched.FullName.Should().Be("John Roe");
        fetched.Email.Should().Be("john@example.com");
    }

    [Fact]
    public async Task Create_owner_with_invalid_email_returns_400_validation_problem()
    {
        var client = await AuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/owners", new
        {
            fullName = "Jane Doe",
            phone = "+5511999999999",
            email = "not-an-email",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
