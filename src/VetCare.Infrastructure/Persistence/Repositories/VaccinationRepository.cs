using VetCare.Domain.Vaccinations;

namespace VetCare.Infrastructure.Persistence.Repositories;

public sealed class VaccinationRepository : EfRepository<Vaccination>
{
    public VaccinationRepository(VetCareDbContext db)
        : base(db)
    {
    }
}
