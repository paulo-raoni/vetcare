namespace VetCare.Domain.Primitives;

public interface ITenantEntity
{
    Guid TenantId { get; }
}
