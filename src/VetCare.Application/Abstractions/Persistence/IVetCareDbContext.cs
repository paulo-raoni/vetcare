using Microsoft.EntityFrameworkCore;
using VetCare.Domain.Owners;
using VetCare.Domain.Pets;
using VetCare.Domain.Tenants;
using VetCare.Domain.Users;

namespace VetCare.Application.Abstractions.Persistence;

public interface IVetCareDbContext
{
    DbSet<Tenant> Tenants { get; }

    DbSet<User> Users { get; }

    DbSet<Owner> Owners { get; }

    DbSet<Pet> Pets { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
