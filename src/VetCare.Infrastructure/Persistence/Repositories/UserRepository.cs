using VetCare.Domain.Users;

namespace VetCare.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : EfRepository<User>
{
    public UserRepository(VetCareDbContext db)
        : base(db)
    {
    }
}
