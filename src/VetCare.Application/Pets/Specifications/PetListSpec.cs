using VetCare.Application.Abstractions.Specifications;
using VetCare.Domain.Pets;

namespace VetCare.Application.Pets.Specifications;

public sealed class PetListSpec : Specification<Pet>
{
    public PetListSpec(int page, int pageSize, Guid? ownerId = null, bool applyPaging = true)
    {
        if (ownerId is not null && ownerId.Value != Guid.Empty)
        {
            Where(p => p.OwnerId == ownerId.Value);
        }

        ApplyOrderBy(p => p.Name);
        if (applyPaging)
        {
            ApplyPaging((page - 1) * pageSize, pageSize);
        }
    }
}
