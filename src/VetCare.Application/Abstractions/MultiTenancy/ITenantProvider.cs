namespace VetCare.Application.Abstractions.MultiTenancy;

public interface ITenantProvider
{
    Guid TenantId { get; }

    bool HasTenant { get; }
}
