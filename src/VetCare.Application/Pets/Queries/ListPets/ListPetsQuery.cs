using MediatR;
using VetCare.Application.Common.Pagination;

namespace VetCare.Application.Pets.Queries.ListPets;

public sealed record ListPetsQuery(int Page = 1, int PageSize = 20, Guid? OwnerId = null) : IRequest<PagedResult<PetDto>>;
