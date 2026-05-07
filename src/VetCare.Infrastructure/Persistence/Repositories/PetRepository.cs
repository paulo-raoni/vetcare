using VetCare.Domain.Pets;

namespace VetCare.Infrastructure.Persistence.Repositories;

public sealed class PetRepository : EfRepository<Pet>
{
    public PetRepository(VetCareDbContext db)
        : base(db)
    {
    }
}
