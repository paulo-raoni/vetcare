namespace VetCare.Application.Auditing;

public sealed record AuditEntry(
    Guid Id,
    Guid? TenantId,
    Guid? UserId,
    string Action,
    string? EntityType,
    Guid? EntityId,
    object? Payload,
    DateTime OccurredAt);
