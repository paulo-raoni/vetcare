using VetCare.Domain.Appointments;

namespace VetCare.Infrastructure.Persistence.Repositories;

public sealed class AppointmentRepository : EfRepository<Appointment>
{
    public AppointmentRepository(VetCareDbContext db)
        : base(db)
    {
    }
}
