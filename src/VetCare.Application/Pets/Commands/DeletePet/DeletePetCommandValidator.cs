using FluentValidation;

namespace VetCare.Application.Pets.Commands.DeletePet;

public sealed class DeletePetCommandValidator : AbstractValidator<DeletePetCommand>
{
    public DeletePetCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
