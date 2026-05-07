namespace VetCare.Application.Auth;

public sealed record AuthResult(string AccessToken, DateTime ExpiresAt, Guid TenantId, Guid UserId, string Email, string Role);
