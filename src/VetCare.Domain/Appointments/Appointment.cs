using VetCare.Domain.Primitives;

namespace VetCare.Domain.Appointments;

public sealed class Appointment : AggregateRoot, ITenantEntity
{
    public const int NotesMaxLength = 1000;

    private Appointment()
    {
        Notes = null;
    }

    public Appointment(
        Guid tenantId,
        Guid petId,
        Guid vetUserId,
        DateTime scheduledAt,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId must not be empty.", nameof(tenantId));
        }

        if (petId == Guid.Empty)
        {
            throw new ArgumentException("PetId must not be empty.", nameof(petId));
        }

        if (vetUserId == Guid.Empty)
        {
            throw new ArgumentException("VetUserId must not be empty.", nameof(vetUserId));
        }

        if (scheduledAt <= DateTime.UtcNow)
        {
            throw new ArgumentException("Appointment must be scheduled in the future.", nameof(scheduledAt));
        }

        if (notes is not null && notes.Length > NotesMaxLength)
        {
            throw new ArgumentException($"Notes must be at most {NotesMaxLength} characters.", nameof(notes));
        }

        TenantId = tenantId;
        PetId = petId;
        VetUserId = vetUserId;
        ScheduledAt = scheduledAt;
        Status = AppointmentStatus.Scheduled;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        AddDomainEvent(new AppointmentScheduledEvent(Id, TenantId, PetId, VetUserId, ScheduledAt));
    }

    public Guid TenantId { get; private set; }

    public Guid PetId { get; private set; }

    public Guid VetUserId { get; private set; }

    public DateTime ScheduledAt { get; private set; }

    public AppointmentStatus Status { get; private set; }

    public string? Notes { get; private set; }

    public void Confirm()
    {
        if (Status != AppointmentStatus.Scheduled)
        {
            throw new DomainException($"Cannot confirm an appointment in status '{Status}'.");
        }

        Status = AppointmentStatus.Confirmed;
        Touch();
    }

    public void Cancel()
    {
        if (Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed)
        {
            throw new DomainException($"Cannot cancel an appointment in status '{Status}'.");
        }

        Status = AppointmentStatus.Cancelled;
        Touch();

        AddDomainEvent(new AppointmentCancelledEvent(Id, TenantId, PetId, VetUserId, ScheduledAt));
    }

    public void Complete()
    {
        if (Status != AppointmentStatus.Confirmed)
        {
            throw new DomainException($"Cannot complete an appointment in status '{Status}'. It must be confirmed first.");
        }

        Status = AppointmentStatus.Completed;
        Touch();

        AddDomainEvent(new AppointmentCompletedEvent(Id, TenantId, PetId, VetUserId, ScheduledAt));
    }
}
