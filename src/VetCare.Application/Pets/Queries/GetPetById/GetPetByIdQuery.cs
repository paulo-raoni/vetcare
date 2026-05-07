using MediatR;

namespace VetCare.Application.Pets.Queries.GetPetById;

public sealed record GetPetByIdQuery(Guid Id) : IRequest<PetDto>;
