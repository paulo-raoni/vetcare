using Microsoft.AspNetCore.Http;
using VetCare.Application.Abstractions.MultiTenancy;

namespace VetCare.Infrastructure.MultiTenancy;

public sealed class CurrentTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid? _override;

    public CurrentTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid TenantId
    {
        get
        {
            if (_override.HasValue)
            {
                return _override.Value;
            }

            return TryReadTenant(out var tenantId) ? tenantId : Guid.Empty;
        }
    }

    public bool HasTenant => _override.HasValue || TryReadTenant(out _);

    public void SetTenant(Guid tenantId)
    {
        _override = tenantId;
    }

    private bool TryReadTenant(out Guid tenantId)
    {
        tenantId = Guid.Empty;
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var claim = user.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out tenantId);
    }
}
