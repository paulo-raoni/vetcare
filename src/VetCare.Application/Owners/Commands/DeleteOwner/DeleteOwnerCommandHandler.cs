using MediatR;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Common.Exceptions;
using VetCare.Application.Owners.Specifications;
using VetCare.Domain.Owners;

namespace VetCare.Application.Owners.Commands.DeleteOwner;

public sealed class DeleteOwnerCommandHandler : IRequestHandler<DeleteOwnerCommand>
{
    private readonly IRepository<Owner> _owners;
    private readonly IVetCareDbContext _db;

    public DeleteOwnerCommandHandler(IRepository<Owner> owners, IVetCareDbContext db)
    {
        _owners = owners;
        _db = db;
    }

    public async Task Handle(DeleteOwnerCommand request, CancellationToken cancellationToken)
    {
        var owner = await _owners.SingleOrDefaultAsync(new OwnerByIdSpec(request.Id), cancellationToken)
            ?? throw new NotFoundException(nameof(Owner), request.Id);

        _owners.Remove(owner);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
