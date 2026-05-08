using MediatR;
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

namespace VetCare.Infrastructure.Persistence;

public sealed class VetCareDbContext : DbContext, IVetCareDbContext
{
    public const string Schema = "vetcare";

    private readonly ITenantProvider _tenantProvider;
    private readonly IPublisher? _publisher;

    public VetCareDbContext(DbContextOptions<VetCareDbContext> options, ITenantProvider tenantProvider, IPublisher? publisher = null)
        : base(options)
    {
        _tenantProvider = tenantProvider;
        _publisher = publisher;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Owner> Owners => Set<Owner>();

    public DbSet<Pet> Pets => Set<Pet>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public DbSet<Vaccination> Vaccinations => Set<Vaccination>();

    internal Guid CurrentTenantId => _tenantProvider.HasTenant ? _tenantProvider.TenantId : Guid.Empty;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();
        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        if (_publisher is not null)
        {
            foreach (var domainEvent in domainEvents)
            {
                await _publisher.Publish(domainEvent, cancellationToken);
            }
        }

        return result;
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
