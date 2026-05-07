using VetCare.Domain.Owners;

namespace VetCare.Infrastructure.Persistence.Repositories;

public sealed class OwnerRepository : EfRepository<Owner>
{
    public OwnerRepository(VetCareDbContext db)
        : base(db)
    {
    }
}
