using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using VetCare.Application.Abstractions.MultiTenancy;

namespace VetCare.Infrastructure.Persistence;

internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<VetCareDbContext>
{
    public VetCareDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("VETCARE_DESIGN_TIME_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=vetcare;Username=vetcare;Password=vetcare";

        var options = new DbContextOptionsBuilder<VetCareDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", VetCareDbContext.Schema))
            .Options;

        return new VetCareDbContext(options, new NullTenantProvider());
    }

    private sealed class NullTenantProvider : ITenantProvider
    {
        public Guid TenantId => Guid.Empty;

        public bool HasTenant => false;
    }
}
