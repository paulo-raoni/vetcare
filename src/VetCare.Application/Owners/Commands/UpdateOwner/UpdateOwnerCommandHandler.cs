using MapsterMapper;
using MediatR;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Common.Exceptions;
using VetCare.Application.Owners.Specifications;
using VetCare.Domain.Owners;

namespace VetCare.Application.Owners.Commands.UpdateOwner;

public sealed class UpdateOwnerCommandHandler : IRequestHandler<UpdateOwnerCommand, OwnerDto>
{
    private readonly IRepository<Owner> _owners;
    private readonly IVetCareDbContext _db;
    private readonly IMapper _mapper;

    public UpdateOwnerCommandHandler(IRepository<Owner> owners, IVetCareDbContext db, IMapper mapper)
    {
        _owners = owners;
        _db = db;
        _mapper = mapper;
    }

    public async Task<OwnerDto> Handle(UpdateOwnerCommand request, CancellationToken cancellationToken)
    {
        var owner = await _owners.SingleOrDefaultAsync(new OwnerByIdSpec(request.Id), cancellationToken)
            ?? throw new NotFoundException(nameof(Owner), request.Id);

        owner.UpdateContact(request.FullName, request.Phone, request.Email);
        await _db.SaveChangesAsync(cancellationToken);

        return _mapper.Map<OwnerDto>(owner);
    }
}
