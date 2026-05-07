using FluentValidation;

namespace VetCare.Application.Owners.Commands.UpdateOwner;

public sealed class UpdateOwnerCommandValidator : AbstractValidator<UpdateOwnerCommand>
{
    public UpdateOwnerCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(160);

        RuleFor(x => x.Phone)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);
    }
}
