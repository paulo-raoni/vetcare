using MediatR;
using VetCare.Application.Abstractions.Messaging;

namespace VetCare.Application.Vaccinations.Commands.UpdateVaccination;

public sealed record UpdateVaccinationCommand(
    Guid Id,
    string VaccineName,
    DateTime AdministeredAt,
    DateTime? NextDueAt,
    string BatchNumber) : IRequest<VaccinationDto>, ICommand;
