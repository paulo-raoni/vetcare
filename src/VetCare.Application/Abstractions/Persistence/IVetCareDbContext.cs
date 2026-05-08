using Microsoft.EntityFrameworkCore;
using VetCare.Domain.Appointments;
using VetCare.Domain.Owners;
using VetCare.Domain.Pets;
using VetCare.Domain.Tenants;
using VetCare.Domain.Users;
using VetCare.Domain.Vaccinations;

namespace VetCare.Application.Abstractions.Persistence;

public interface IVetCareDbContext
{
    DbSet<Tenant> Tenants { get; }

    DbSet<User> Users { get; }

    DbSet<Owner> Owners { get; }

    DbSet<Pet> Pets { get; }

    DbSet<Appointment> Appointments { get; }

    DbSet<Vaccination> Vaccinations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
