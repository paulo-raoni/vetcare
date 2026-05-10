using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VetCare.Application.Abstractions.MultiTenancy;
using VetCare.Domain.Owners;
using VetCare.Domain.Pets;
using VetCare.Domain.Tenants;
using VetCare.Infrastructure.Outbox;
using VetCare.Infrastructure.Persistence;

namespace VetCare.Infrastructure.IntegrationTests.Outbox;

[Collection(IntegrationTestsCollection.Name)]
public sealed class OutboxWritePathTests
{
    private readonly VetCareWebApplicationFactory _factory;

    public OutboxWritePathTests(VetCareWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SaveChangesAsync_with_one_domain_event_writes_one_outbox_row()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VetCareDbContext>();

        var tenant = await SeedTenantAsync(db);

        var owner = new Owner(tenant.Id, "Jane Doe", "+5511999999999", $"jane-{Guid.NewGuid():N}@test");
        db.Owners.Add(owner);

        var beforeCount = await db.OutboxMessages.CountAsync();
        await db.SaveChangesAsync();
        var afterCount = await db.OutboxMessages.CountAsync();

        (afterCount - beforeCount).Should().Be(1);

        var row = await db.OutboxMessages
            .OrderByDescending(o => o.OccurredOnUtc)
            .FirstAsync();

        row.Type.Should().Contain(nameof(OwnerCreatedEvent));
        row.Content.Should().Contain(owner.Id.ToString());
        row.TenantId.Should().Be(tenant.Id);
        row.ProcessedOnUtc.Should().BeNull();
        row.Error.Should().BeNull();
        row.Attempts.Should().Be(0);
    }

    [Fact]
    public async Task SaveChangesAsync_outbox_row_uses_aggregate_TenantId_not_ambient_provider()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VetCareDbContext>();
        var ambient = scope.ServiceProvider.GetRequiredService<ITenantProvider>();

        // Guard the invariant the bug exploited: in a DI-direct-resolve
        // path with no HttpContext, the ambient provider has no tenant.
        // If this guard ever flips, the test below is no longer
        // exercising the scenario it claims to.
        ambient.HasTenant.Should().BeFalse();

        var tenant = await SeedTenantAsync(db);

        var owner = new Owner(
            tenant.Id,
            "Tenant-Source Owner",
            "+5511999999998",
            $"tenant-source-{Guid.NewGuid():N}@test");
        db.Owners.Add(owner);

        await db.SaveChangesAsync();

        var row = await db.OutboxMessages
            .Where(o => o.Type.Contains(nameof(OwnerCreatedEvent)))
            .OrderByDescending(o => o.OccurredOnUtc)
            .FirstAsync();

        row.TenantId.Should().Be(tenant.Id);
        row.TenantId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task SaveChangesAsync_with_no_domain_events_writes_no_outbox_rows()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VetCareDbContext>();

        await SeedTenantAsync(db);

        var beforeCount = await db.OutboxMessages.CountAsync();
        var result = await db.SaveChangesAsync();
        var afterCount = await db.OutboxMessages.CountAsync();

        result.Should().Be(0);
        afterCount.Should().Be(beforeCount);
    }

    [Fact]
    public async Task SaveChangesAsync_with_multiple_domain_events_writes_one_outbox_row_per_event()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VetCareDbContext>();

        var tenant = await SeedTenantAsync(db);

        var owner = new Owner(tenant.Id, "Owner A", "+5511999999990", $"a-{Guid.NewGuid():N}@test");
        var pet = new Pet(tenant.Id, owner.Id, "Rex", Species.Dog, "Labrador", new DateOnly(2020, 1, 1));
        db.Owners.Add(owner);
        db.Pets.Add(pet);

        var beforeCount = await db.OutboxMessages.CountAsync();
        await db.SaveChangesAsync();
        var afterCount = await db.OutboxMessages.CountAsync();

        (afterCount - beforeCount).Should().Be(2);
    }

    private static async Task<Tenant> SeedTenantAsync(VetCareDbContext db)
    {
        var tenant = new Tenant($"Clinic {Guid.NewGuid():N}", $"clinic-{Guid.NewGuid():N}");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return tenant;
    }
}
