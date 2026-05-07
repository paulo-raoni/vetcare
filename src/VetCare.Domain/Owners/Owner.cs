using VetCare.Domain.Primitives;

namespace VetCare.Domain.Owners;

public sealed class Owner : AggregateRoot, ITenantEntity
{
    private Owner()
    {
        FullName = string.Empty;
        Phone = string.Empty;
        Email = string.Empty;
    }

    public Owner(Guid tenantId, string fullName, string phone, string email)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId must not be empty.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Owner full name must not be empty.", nameof(fullName));
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new ArgumentException("Owner phone must not be empty.", nameof(phone));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Owner email must not be empty.", nameof(email));
        }

        TenantId = tenantId;
        FullName = fullName.Trim();
        Phone = phone.Trim();
        Email = email.Trim().ToLowerInvariant();

        AddDomainEvent(new OwnerCreatedEvent(Id, TenantId, FullName));
    }

    public Guid TenantId { get; private set; }

    public string FullName { get; private set; }

    public string Phone { get; private set; }

    public string Email { get; private set; }

    public void UpdateContact(string fullName, string phone, string email)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Owner full name must not be empty.", nameof(fullName));
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new ArgumentException("Owner phone must not be empty.", nameof(phone));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Owner email must not be empty.", nameof(email));
        }

        FullName = fullName.Trim();
        Phone = phone.Trim();
        Email = email.Trim().ToLowerInvariant();
        Touch();
    }
}
