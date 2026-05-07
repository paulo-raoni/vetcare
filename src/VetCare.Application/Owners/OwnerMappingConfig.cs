using Mapster;
using VetCare.Domain.Owners;

namespace VetCare.Application.Owners;

public sealed class OwnerMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Owner, OwnerDto>();
    }
}
