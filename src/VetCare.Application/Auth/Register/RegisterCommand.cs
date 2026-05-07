using MediatR;

namespace VetCare.Application.Auth.Register;

public sealed record RegisterCommand(
    string TenantName,
    string TenantSlug,
    string Email,
    string Password) : IRequest<AuthResult>;
