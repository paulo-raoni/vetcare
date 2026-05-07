using Microsoft.AspNetCore.Http;
using VetCare.Application.Abstractions.MultiTenancy;

namespace VetCare.Infrastructure.MultiTenancy;

public sealed class CurrentTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid TenantId
    {
        get
        {
            if (TryReadTenant(out var tenantId))
            {
                return tenantId;
            }

            return Guid.Empty;
        }
    }

    public bool HasTenant => TryReadTenant(out _);

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
