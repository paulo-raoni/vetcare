using VetCare.Application.Abstractions.Specifications;
using VetCare.Domain.Pets;

namespace VetCare.Application.Pets.Specifications;

public sealed class PetsByOwnerSpec : Specification<Pet>
{
    public PetsByOwnerSpec(Guid ownerId)
    {
        Where(p => p.OwnerId == ownerId);
        ApplyOrderBy(p => p.Name);
    }
}
