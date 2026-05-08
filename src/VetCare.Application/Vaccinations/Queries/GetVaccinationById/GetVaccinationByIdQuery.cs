using MediatR;

namespace VetCare.Application.Vaccinations.Queries.GetVaccinationById;

public sealed record GetVaccinationByIdQuery(Guid Id) : IRequest<VaccinationDto>;
