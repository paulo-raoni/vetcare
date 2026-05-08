using VetCare.Domain.Appointments;

namespace VetCare.Application.Appointments;

public sealed record AppointmentDto(
    Guid Id,
    Guid TenantId,
    Guid PetId,
    Guid VetUserId,
    DateTime ScheduledAt,
    AppointmentStatus Status,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt);
