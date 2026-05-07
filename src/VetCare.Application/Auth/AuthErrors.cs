namespace VetCare.Application.Auth;

public class AuthException : Exception
{
    public AuthException(string message)
        : base(message)
    {
    }
}

public sealed class TenantSlugAlreadyExistsException : AuthException
{
    public TenantSlugAlreadyExistsException(string slug)
        : base($"Tenant slug '{slug}' is already in use.")
    {
        Slug = slug;
    }

    public string Slug { get; }
}

public sealed class InvalidCredentialsException : AuthException
{
    public InvalidCredentialsException()
        : base("Invalid credentials.")
    {
    }
}
