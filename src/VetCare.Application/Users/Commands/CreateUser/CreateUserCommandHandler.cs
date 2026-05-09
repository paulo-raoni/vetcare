using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VetCare.Application.Abstractions.Identity;
using VetCare.Application.Abstractions.MultiTenancy;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Domain.Users;

namespace VetCare.Application.Users.Commands.CreateUser;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IVetCareDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly IPasswordHasher _hasher;
    private readonly IMapper _mapper;

    public CreateUserCommandHandler(
        IVetCareDbContext db,
        ITenantProvider tenantProvider,
        IPasswordHasher hasher,
        IMapper mapper)
    {
        _db = db;
        _tenantProvider = tenantProvider;
        _hasher = hasher;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.HasTenant)
        {
            throw new InvalidOperationException("A tenant context is required to create a user.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var emailExists = await _db.Users
            .AnyAsync(u => u.Email == email, cancellationToken);

        if (emailExists)
        {
            throw new EmailAlreadyInUseException(email);
        }

        var hash = _hasher.Hash(request.Password);
        var user = new User(_tenantProvider.TenantId, email, hash, request.Role);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserDto>(user);
    }
}
