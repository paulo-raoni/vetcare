using FluentValidation;

namespace VetCare.Application.Pets.Commands.CreatePet;

public sealed class CreatePetCommandValidator : AbstractValidator<CreatePetCommand>
{
    public CreatePetCommandValidator()
    {
        RuleFor(x => x.OwnerId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(80);

        RuleFor(x => x.Species).IsInEnum();

        RuleFor(x => x.Breed)
            .NotEmpty()
            .MaximumLength(80);

        RuleFor(x => x.DateOfBirth)
            .Must(dob => dob <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth must not be in the future.");

        RuleFor(x => x.PhotoUrl)
            .MaximumLength(512);
    }
}
