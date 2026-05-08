using VetCare.Application.Abstractions.Specifications;
using VetCare.Domain.Appointments;

namespace VetCare.Application.Appointments.Specifications;

public sealed class AppointmentListSpec : Specification<Appointment>
{
    public AppointmentListSpec(
        int page,
        int pageSize,
        Guid? petId = null,
        AppointmentStatus? status = null,
        DateTime? from = null,
        DateTime? to = null,
        bool applyPaging = true)
    {
        var hasPetFilter = petId is not null && petId.Value != Guid.Empty;
        var petIdValue = petId ?? Guid.Empty;
        var hasStatusFilter = status is not null;
        var statusValue = status ?? default;
        var hasFromFilter = from is not null;
        var fromValue = from ?? DateTime.MinValue;
        var hasToFilter = to is not null;
        var toValue = to ?? DateTime.MaxValue;

        Where(a =>
            (!hasPetFilter || a.PetId == petIdValue)
            && (!hasStatusFilter || a.Status == statusValue)
            && (!hasFromFilter || a.ScheduledAt >= fromValue)
            && (!hasToFilter || a.ScheduledAt <= toValue));

        ApplyOrderBy(a => a.ScheduledAt);

        if (applyPaging)
        {
            ApplyPaging((page - 1) * pageSize, pageSize);
        }
    }
}
