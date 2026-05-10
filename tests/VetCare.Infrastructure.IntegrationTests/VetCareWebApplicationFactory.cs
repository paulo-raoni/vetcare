using Amazon.S3;
using Amazon.SQS;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute;
using NSubstitute.ClearExtensions;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;
using VetCare.Application.Abstractions.Identity;
using VetCare.Application.Abstractions.Messaging;
using VetCare.Application.Abstractions.Storage;
using VetCare.Domain.Users;
using VetCare.Infrastructure.Persistence;

namespace VetCare.Infrastructure.IntegrationTests;

public sealed class VetCareWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string AuditCollectionName = "audit_log";

    private readonly string _mongoDatabaseName = $"vetcare-tests-{Guid.NewGuid():N}";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("vetcare")
        .WithUsername("vetcare")
        .WithPassword("vetcare")
        .Build();

    private readonly MongoDbContainer _mongo = new MongoDbBuilder()
        .WithImage("mongo:7")
        .Build();

    public ISqsPublisher SqsPublisher { get; } = Substitute.For<ISqsPublisher>();

    public IStorageService StorageService { get; } = Substitute.For<IStorageService>();

    public void ClearSubstitutes()
    {
        SqsPublisher.ClearReceivedCalls();
        SqsPublisher.ClearSubstitute(ClearOptions.ReturnValues);
        StorageService.ClearReceivedCalls();
        StorageService.ClearSubstitute(ClearOptions.ReturnValues);
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _mongo.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VetCareDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
        await _mongo.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),
                ["Jwt:Secret"] = "integration-test-secret-X7$kP3@nE9!aL5Z2bM%qFwY8&jR-vU#oT",
                ["Jwt:Issuer"] = "vetcare-tests",
                ["Jwt:Audience"] = "vetcare-tests",
                ["Jwt:ExpiryMinutes"] = "30",
                ["Aws:Region"] = "us-east-1",
                ["Aws:ServiceUrl"] = "http://localhost:4566",
                ["Aws:AccessKey"] = "test",
                ["Aws:SecretKey"] = "test",
                ["S3:BucketName"] = "vetcare-pets-tests",
                ["S3:ServiceUrl"] = "http://localhost:4566",
                ["Mongo:ConnectionString"] = _mongo.GetConnectionString(),
                ["Mongo:DatabaseName"] = _mongoDatabaseName,
                ["Mongo:AuditCollectionName"] = AuditCollectionName,
                ["Outbox:PollInterval"] = "01:00:00",
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
                options.UseNpgsql(
                    _postgres.GetConnectionString(),
                    npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", VetCareDbContext.Schema)));

            ReplaceService(services, typeof(IAmazonSQS), Substitute.For<IAmazonSQS>());
            ReplaceService(services, typeof(ISqsPublisher), SqsPublisher);
            ReplaceService(services, typeof(IAmazonS3), Substitute.For<IAmazonS3>());
            ReplaceService(services, typeof(IStorageService), StorageService);
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

    public async Task<Guid> CreateUserAsync(Guid tenantId, UserRole role, string? email = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VetCareDbContext>();
        var user = new User(tenantId, email ?? $"user-{Guid.NewGuid():N}@vetcare.test", "hash", role);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    public async Task<(Guid UserId, string AccessToken)> CreateUserAndIssueTokenAsync(Guid tenantId, UserRole role)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VetCareDbContext>();
        var tokens = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var user = new User(tenantId, $"user-{Guid.NewGuid():N}@vetcare.test", "hash", role);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var token = tokens.Generate(user);
        return (user.Id, token.Token);
    }

    public async Task<long> CountAuditEntriesByActionAsync(string action, CancellationToken ct = default)
    {
        using var scope = Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        var collection = database.GetCollection<BsonDocument>(AuditCollectionName);
        var filter = Builders<BsonDocument>.Filter.Eq("action", action);
        return await collection.CountDocumentsAsync(filter, cancellationToken: ct);
    }

    public async Task<long> CountAuditEntriesByActionAndEntityAsync(string action, Guid entityId, CancellationToken ct = default)
    {
        using var scope = Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        var collection = database.GetCollection<BsonDocument>(AuditCollectionName);
        var idString = entityId.ToString();
        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("action", action),
            Builders<BsonDocument>.Filter.Eq("payload.petId", idString));
        return await collection.CountDocumentsAsync(filter, cancellationToken: ct);
    }
}
