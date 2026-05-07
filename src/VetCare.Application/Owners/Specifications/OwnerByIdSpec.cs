using VetCare.Application.Abstractions.Specifications;
using VetCare.Domain.Owners;

namespace VetCare.Application.Owners.Specifications;

public sealed class OwnerByIdSpec : Specification<Owner>
{
    public OwnerByIdSpec(Guid ownerId)
    {
        Where(o => o.Id == ownerId);
    }
}
