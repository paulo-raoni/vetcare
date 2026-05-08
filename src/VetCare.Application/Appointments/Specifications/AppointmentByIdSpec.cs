using VetCare.Application.Abstractions.Specifications;
using VetCare.Domain.Appointments;

namespace VetCare.Application.Appointments.Specifications;

public sealed class AppointmentByIdSpec : Specification<Appointment>
{
    public AppointmentByIdSpec(Guid appointmentId)
    {
        Where(a => a.Id == appointmentId);
    }
}
