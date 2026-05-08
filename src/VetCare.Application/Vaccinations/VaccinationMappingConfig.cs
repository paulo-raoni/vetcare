using Mapster;
using VetCare.Domain.Vaccinations;

namespace VetCare.Application.Vaccinations;

public sealed class VaccinationMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Vaccination, VaccinationDto>();
    }
}
