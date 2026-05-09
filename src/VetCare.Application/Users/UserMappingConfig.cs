using Mapster;
using VetCare.Domain.Users;

namespace VetCare.Application.Users;

public sealed class UserMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<User, UserDto>();
    }
}
