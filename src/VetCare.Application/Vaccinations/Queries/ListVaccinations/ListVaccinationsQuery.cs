using MediatR;
using VetCare.Application.Common.Pagination;

namespace VetCare.Application.Vaccinations.Queries.ListVaccinations;

public sealed record ListVaccinationsQuery(int Page = 1, int PageSize = 20, Guid? PetId = null) : IRequest<PagedResult<VaccinationDto>>;
