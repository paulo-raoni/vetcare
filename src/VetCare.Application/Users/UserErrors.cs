namespace VetCare.Application.Users;

public sealed class EmailAlreadyInUseException : Exception
{
    public EmailAlreadyInUseException(string email)
        : base($"Email '{email}' is already in use within this tenant.")
    {
        Email = email;
    }

    public string Email { get; }
}
