using Amazon.SQS;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using VetCare.Application.Abstractions.Messaging;
using VetCare.Infrastructure.Persistence;

namespace VetCare.Infrastructure.IntegrationTests;

public sealed class VetCareWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"vetcare-tests-{Guid.NewGuid():N}";

    public ISqsPublisher SqsPublisher { get; } = Substitute.For<ISqsPublisher>();

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
                ["Aws:Region"] = "us-east-1",
                ["Aws:ServiceUrl"] = "http://localhost:4566",
                ["Aws:AccessKey"] = "test",
                ["Aws:SecretKey"] = "test",
            });
        });

        builder.ConfigureServices(services =>
        {
            var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<VetCareDbContext>));
            if (dbDescriptor is not null)
            {
                services.Remove(dbDescriptor);
            }

            services.AddDbContext<VetCareDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            var sqsDescriptors = services.Where(d => d.ServiceType == typeof(IAmazonSQS)).ToList();
            foreach (var descriptor in sqsDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddSingleton(Substitute.For<IAmazonSQS>());

            var publisherDescriptors = services.Where(d => d.ServiceType == typeof(ISqsPublisher)).ToList();
            foreach (var descriptor in publisherDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddSingleton(SqsPublisher);
        });
    }
}
