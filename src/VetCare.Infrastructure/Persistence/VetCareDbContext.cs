using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VetCare.Application.Abstractions.MultiTenancy;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Domain.Appointments;
using VetCare.Domain.Owners;
using VetCare.Domain.Pets;
using VetCare.Domain.Primitives;
using VetCare.Domain.Tenants;
using VetCare.Domain.Users;
using VetCare.Domain.Vaccinations;
using VetCare.Infrastructure.Outbox;

namespace VetCare.Infrastructure.Persistence;

public sealed class VetCareDbContext : DbContext, IVetCareDbContext
{
    public const string Schema = "vetcare";

    private readonly ITenantProvider _tenantProvider;

    public VetCareDbContext(DbContextOptions<VetCareDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Owner> Owners => Set<Owner>();

    public DbSet<Pet> Pets => Set<Pet>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public DbSet<Vaccination> Vaccinations => Set<Vaccination>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    internal Guid CurrentTenantId => _tenantProvider.HasTenant ? _tenantProvider.TenantId : Guid.Empty;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        if (aggregates.Count > 0)
        {
            foreach (var aggregate in aggregates)
            {
                var aggregateTenantId = aggregate is ITenantEntity tenantEntity
                    ? tenantEntity.TenantId
                    : (_tenantProvider.HasTenant ? _tenantProvider.TenantId : Guid.Empty);

                foreach (var domainEvent in aggregate.DomainEvents)
                {
                    var eventType = domainEvent.GetType();
                    OutboxMessages.Add(new OutboxMessage
                    {
                        Id = domainEvent.EventId,
                        OccurredOnUtc = domainEvent.OccurredOnUtc,
                        Type = eventType.AssemblyQualifiedName!,
                        Content = JsonSerializer.Serialize(domainEvent, eventType),
                        TenantId = aggregateTenantId,
                        ProcessedOnUtc = null,
                        Error = null,
                        Attempts = 0,
                    });
                }
            }

            foreach (var aggregate in aggregates)
            {
                aggregate.ClearDomainEvents();
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (Database.IsRelational())
        {
            modelBuilder.HasDefaultSchema(Schema);
        }

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VetCareDbContext).Assembly);

        modelBuilder.Entity<User>().HasQueryFilter(u => u.TenantId == CurrentTenantId);
        modelBuilder.Entity<Owner>().HasQueryFilter(o => o.TenantId == CurrentTenantId);
        modelBuilder.Entity<Pet>().HasQueryFilter(p => p.TenantId == CurrentTenantId);
        modelBuilder.Entity<Appointment>().HasQueryFilter(a => a.TenantId == CurrentTenantId);
        modelBuilder.Entity<Vaccination>().HasQueryFilter(v => v.TenantId == CurrentTenantId);

        base.OnModelCreating(modelBuilder);
    }
}
