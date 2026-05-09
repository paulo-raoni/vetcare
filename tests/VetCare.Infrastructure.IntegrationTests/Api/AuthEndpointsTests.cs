using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace VetCare.Infrastructure.IntegrationTests.Api;

[Collection(IntegrationTestsCollection.Name)]
public sealed class AuthEndpointsTests
{
    private readonly VetCareWebApplicationFactory _factory;

    public AuthEndpointsTests(VetCareWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private sealed record AuthResponse(string AccessToken, DateTime ExpiresAt, Guid TenantId, Guid UserId, string Email, string Role);

    [Fact]
    public async Task Register_returns_200_with_token()
    {
        var client = _factory.CreateClient();
        var slug = $"clinic-{Guid.NewGuid():N}";

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantName = "Acme Vet",
            tenantSlug = slug,
            email = "admin@acme.test",
            password = "ChangeMe123!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.TenantId.Should().NotBeEmpty();
        body.UserId.Should().NotBeEmpty();
        body.Email.Should().Be("admin@acme.test");
        body.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task Login_after_register_returns_200_with_token()
    {
        var client = _factory.CreateClient();
        var slug = $"clinic-{Guid.NewGuid():N}";
        const string email = "vet@acme.test";
        const string password = "ChangeMe123!";

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantName = "Acme Vet",
            tenantSlug = slug,
            email,
            password,
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            tenantSlug = slug,
            email,
            password,
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        var client = _factory.CreateClient();
        var slug = $"clinic-{Guid.NewGuid():N}";

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantName = "Acme Vet",
            tenantSlug = slug,
            email = "vet2@acme.test",
            password = "ChangeMe123!",
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            tenantSlug = slug,
            email = "vet2@acme.test",
            password = "WrongPassword!",
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
