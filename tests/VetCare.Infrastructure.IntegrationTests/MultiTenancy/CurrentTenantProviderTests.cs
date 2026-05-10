using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using VetCare.Infrastructure.MultiTenancy;

namespace VetCare.Infrastructure.IntegrationTests.MultiTenancy;

public sealed class CurrentTenantProviderTests
{
    [Fact]
    public void SetTenant_overrides_http_context_lookup()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);

        var provider = new CurrentTenantProvider(accessor);
        provider.HasTenant.Should().BeFalse();
        provider.TenantId.Should().Be(Guid.Empty);

        var explicitTenant = Guid.NewGuid();
        provider.SetTenant(explicitTenant);

        provider.HasTenant.Should().BeTrue();
        provider.TenantId.Should().Be(explicitTenant);
    }
}
