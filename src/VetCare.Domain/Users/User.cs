using VetCare.Domain.Primitives;

namespace VetCare.Domain.Users;

public sealed class User : AggregateRoot, ITenantEntity
{
    private User()
    {
        Email = string.Empty;
        PasswordHash = string.Empty;
    }

    public User(Guid tenantId, string email, string passwordHash, UserRole role)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId must not be empty.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email must not be empty.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash must not be empty.", nameof(passwordHash));
        }

        TenantId = tenantId;
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        Role = role;
    }

    public Guid TenantId { get; private set; }

    public string Email { get; private set; }

    public string PasswordHash { get; private set; }

    public UserRole Role { get; private set; }
}
