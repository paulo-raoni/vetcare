using FluentValidation;

namespace VetCare.Application.Owners.Commands.DeleteOwner;

public sealed class DeleteOwnerCommandValidator : AbstractValidator<DeleteOwnerCommand>
{
    public DeleteOwnerCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
