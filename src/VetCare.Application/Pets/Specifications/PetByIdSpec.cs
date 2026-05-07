using VetCare.Application.Abstractions.Specifications;
using VetCare.Domain.Pets;

namespace VetCare.Application.Pets.Specifications;

public sealed class PetByIdSpec : Specification<Pet>
{
    public PetByIdSpec(Guid petId)
    {
        Where(p => p.Id == petId);
    }
}
