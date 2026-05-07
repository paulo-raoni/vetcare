using MediatR;

namespace VetCare.Application.Auth.Login;

public sealed record LoginCommand(string TenantSlug, string Email, string Password) : IRequest<AuthResult>;
