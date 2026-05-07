using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VetCare.Infrastructure.Persistence;

namespace VetCare.Infrastructure.IntegrationTests;

public sealed class VetCareWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"vetcare-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=ignored;Username=ignored;Password=ignored",
                ["Jwt:Secret"] = "test-secret-test-secret-test-secret-test-secret-32bytes!!",
                ["Jwt:Issuer"] = "vetcare-tests",
                ["Jwt:Audience"] = "vetcare-tests",
                ["Jwt:ExpiryMinutes"] = "30",
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<VetCareDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<VetCareDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
