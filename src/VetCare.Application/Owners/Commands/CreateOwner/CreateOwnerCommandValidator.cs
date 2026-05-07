using FluentValidation;

namespace VetCare.Application.Owners.Commands.CreateOwner;

public sealed class CreateOwnerCommandValidator : AbstractValidator<CreateOwnerCommand>
{
    public CreateOwnerCommandValidator()
    {
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
