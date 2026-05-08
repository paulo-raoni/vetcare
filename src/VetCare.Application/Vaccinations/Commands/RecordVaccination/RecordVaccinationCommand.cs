using MediatR;

namespace VetCare.Application.Vaccinations.Commands.RecordVaccination;

public sealed record RecordVaccinationCommand(
    Guid PetId,
    string VaccineName,
    DateTime AdministeredAt,
    DateTime? NextDueAt,
    string BatchNumber) : IRequest<VaccinationDto>;
