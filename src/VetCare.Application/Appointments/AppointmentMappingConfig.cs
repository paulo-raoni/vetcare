using Mapster;
using VetCare.Domain.Appointments;

namespace VetCare.Application.Appointments;

public sealed class AppointmentMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Appointment, AppointmentDto>();
    }
}
