using MapsterMapper;
using MediatR;
using VetCare.Application.Abstractions.MultiTenancy;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Domain.Owners;

namespace VetCare.Application.Owners.Commands.CreateOwner;

public sealed class CreateOwnerCommandHandler : IRequestHandler<CreateOwnerCommand, OwnerDto>
{
    private readonly IRepository<Owner> _owners;
    private readonly IVetCareDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly IMapper _mapper;

    public CreateOwnerCommandHandler(
        IRepository<Owner> owners,
        IVetCareDbContext db,
        ITenantProvider tenantProvider,
        IMapper mapper)
    {
        _owners = owners;
        _db = db;
        _tenantProvider = tenantProvider;
        _mapper = mapper;
    }

    public async Task<OwnerDto> Handle(CreateOwnerCommand request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.HasTenant)
        {
            throw new InvalidOperationException("A tenant context is required to create an owner.");
        }

        var owner = new Owner(_tenantProvider.TenantId, request.FullName, request.Phone, request.Email);
        _owners.Add(owner);
        await _db.SaveChangesAsync(cancellationToken);

        return _mapper.Map<OwnerDto>(owner);
    }
}
