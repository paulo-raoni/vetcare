using Microsoft.EntityFrameworkCore;
using VetCare.Application.Abstractions.MultiTenancy;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Domain.Owners;
using VetCare.Domain.Pets;
using VetCare.Domain.Tenants;
using VetCare.Domain.Users;

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

    internal Guid CurrentTenantId => _tenantProvider.HasTenant ? _tenantProvider.TenantId : Guid.Empty;

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

        base.OnModelCreating(modelBuilder);
    }
}
