using VetCare.Application.Abstractions.Specifications;
using VetCare.Domain.Users;

namespace VetCare.Application.Users.Specifications;

public sealed class UserByIdSpec : Specification<User>
{
    public UserByIdSpec(Guid userId)
    {
        Where(u => u.Id == userId);
    }
}
