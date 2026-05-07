using MediatR;
using Microsoft.EntityFrameworkCore;
using VetCare.Application.Abstractions.Identity;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Domain.Tenants;
using VetCare.Domain.Users;

namespace VetCare.Application.Auth.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResult>
{
    private readonly IVetCareDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _tokens;

    public RegisterCommandHandler(IVetCareDbContext db, IPasswordHasher hasher, IJwtTokenService tokens)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<AuthResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TenantName)
            || string.IsNullOrWhiteSpace(request.TenantSlug)
            || string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("All registration fields are required.");
        }

        if (request.Password.Length < 8)
        {
            throw new ArgumentException("Password must be at least 8 characters long.");
        }

        var slug = request.TenantSlug.Trim().ToLowerInvariant();
        var slugExists = await _db.Tenants
            .IgnoreQueryFilters()
            .AnyAsync(t => t.Slug == slug, cancellationToken);

        if (slugExists)
        {
            throw new TenantSlugAlreadyExistsException(slug);
        }

        var tenant = new Tenant(request.TenantName, slug);
        _db.Tenants.Add(tenant);

        var hash = _hasher.Hash(request.Password);
        var user = new User(tenant.Id, request.Email, hash, UserRole.Admin);
        _db.Users.Add(user);

        await _db.SaveChangesAsync(cancellationToken);

        var token = _tokens.Generate(user);
        return new AuthResult(token.Token, token.ExpiresAt, tenant.Id, user.Id, user.Email, user.Role.ToString());
    }
}
