using VetCare.Domain.Primitives;

namespace VetCare.Domain.Tenants;

public sealed class Tenant : AggregateRoot
{
    private Tenant()
    {
        Name = string.Empty;
        Slug = string.Empty;
    }

    public Tenant(string name, string slug)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tenant name must not be empty.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Tenant slug must not be empty.", nameof(slug));
        }

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        IsActive = true;
    }

    public string Name { get; private set; }

    public string Slug { get; private set; }

    public bool IsActive { get; private set; }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    public void Activate()
    {
        IsActive = true;
        Touch();
    }
}
