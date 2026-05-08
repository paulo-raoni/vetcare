using VetCare.Application.Abstractions.Specifications;
using VetCare.Domain.Vaccinations;

namespace VetCare.Application.Vaccinations.Specifications;

public sealed class VaccinationByIdSpec : Specification<Vaccination>
{
    public VaccinationByIdSpec(Guid vaccinationId)
    {
        Where(v => v.Id == vaccinationId);
    }
}
