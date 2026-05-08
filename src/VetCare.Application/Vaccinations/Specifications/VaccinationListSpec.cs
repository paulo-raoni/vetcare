using VetCare.Application.Abstractions.Specifications;
using VetCare.Domain.Vaccinations;

namespace VetCare.Application.Vaccinations.Specifications;

public sealed class VaccinationListSpec : Specification<Vaccination>
{
    public VaccinationListSpec(int page, int pageSize, Guid? petId = null, bool applyPaging = true)
    {
        if (petId is not null && petId.Value != Guid.Empty)
        {
            var petIdValue = petId.Value;
            Where(v => v.PetId == petIdValue);
        }

        ApplyOrderByDescending(v => v.AdministeredAt);

        if (applyPaging)
        {
            ApplyPaging((page - 1) * pageSize, pageSize);
        }
    }
}
