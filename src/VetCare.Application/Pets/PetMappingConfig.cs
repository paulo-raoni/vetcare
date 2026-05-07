using Mapster;
using VetCare.Domain.Pets;

namespace VetCare.Application.Pets;

public sealed class PetMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Pet, PetDto>();
    }
}
