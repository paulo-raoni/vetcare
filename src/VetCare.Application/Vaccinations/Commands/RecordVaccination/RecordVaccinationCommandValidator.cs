using FluentValidation;
using VetCare.Domain.Vaccinations;

namespace VetCare.Application.Vaccinations.Commands.RecordVaccination;

public sealed class RecordVaccinationCommandValidator : AbstractValidator<RecordVaccinationCommand>
{
    public RecordVaccinationCommandValidator()
    {
        RuleFor(x => x.PetId).NotEmpty();

        RuleFor(x => x.VaccineName)
            .NotEmpty()
            .MaximumLength(Vaccination.VaccineNameMaxLength);

        RuleFor(x => x.BatchNumber)
            .NotEmpty()
            .MaximumLength(Vaccination.BatchNumberMaxLength);

        RuleFor(x => x.AdministeredAt)
            .Must(d => d <= DateTime.UtcNow)
            .WithMessage("AdministeredAt must not be in the future.");

        RuleFor(x => x)
            .Must(x => x.NextDueAt is null || x.NextDueAt.Value >= x.AdministeredAt)
            .WithMessage("NextDueAt must be on or after AdministeredAt.");
    }
}
