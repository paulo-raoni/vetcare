using MediatR;
using VetCare.Application.Common.Pagination;

namespace VetCare.Application.Owners.Queries.ListOwners;

public sealed record ListOwnersQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<OwnerDto>>;
