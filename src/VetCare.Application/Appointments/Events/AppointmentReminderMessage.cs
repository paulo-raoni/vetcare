namespace VetCare.Application.Appointments.Events;

public sealed record AppointmentReminderMessage(
    Guid AppointmentId,
    Guid TenantId,
    Guid PetId,
    Guid VetUserId,
    DateTime ScheduledAt,
    string Type);
