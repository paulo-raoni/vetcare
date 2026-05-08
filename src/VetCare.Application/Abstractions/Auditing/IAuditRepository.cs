using VetCare.Application.Auditing;

namespace VetCare.Application.Abstractions.Auditing;

public interface IAuditRepository
{
    Task SaveAsync(AuditEntry entry, CancellationToken ct);
}
