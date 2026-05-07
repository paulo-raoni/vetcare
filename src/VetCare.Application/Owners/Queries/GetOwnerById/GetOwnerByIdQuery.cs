using MediatR;

namespace VetCare.Application.Owners.Queries.GetOwnerById;

public sealed record GetOwnerByIdQuery(Guid Id) : IRequest<OwnerDto>;
