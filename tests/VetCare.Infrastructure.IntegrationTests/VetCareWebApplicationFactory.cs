using Amazon.S3;
using Amazon.SQS;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NSubstitute;
using VetCare.Application.Abstractions.Auditing;
using VetCare.Application.Abstractions.Messaging;
using VetCare.Application.Abstractions.Storage;
using VetCare.Infrastructure.Persistence;

namespace VetCare.Infrastructure.IntegrationTests;

public sealed class VetCareWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"vetcare-tests-{Guid.NewGuid():N}";

    public ISqsPublisher SqsPublisher { get; } = Substitute.For<ISqsPublisher>();

    public IAuditRepository AuditRepository { get; } = Substitute.For<IAuditRepository>();

    public IStorageService StorageService { get; } = Substitute.For<IStorageService>();

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
                ["S3:BucketName"] = "vetcare-pets-tests",
                ["S3:ServiceUrl"] = "http://localhost:4566",
                ["Mongo:ConnectionString"] = "mongodb://localhost:27017",
                ["Mongo:DatabaseName"] = "vetcare-tests",
                ["Mongo:AuditCollectionName"] = "audit_log",
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

            ReplaceService(services, typeof(IAmazonS3), Substitute.For<IAmazonS3>());
            ReplaceService(services, typeof(IStorageService), StorageService);
            ReplaceService(services, typeof(IMongoClient), Substitute.For<IMongoClient>());
            ReplaceService(services, typeof(IMongoDatabase), Substitute.For<IMongoDatabase>());
            ReplaceService(services, typeof(IAuditRepository), AuditRepository);
        });
    }

    private static void ReplaceService(IServiceCollection services, Type serviceType, object instance)
    {
        var descriptors = services.Where(d => d.ServiceType == serviceType).ToList();
        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }

        services.AddSingleton(serviceType, instance);
    }
}
