using FluentValidation;

namespace VetCare.Application.Owners.Queries.GetOwnerById;

public sealed class GetOwnerByIdQueryValidator : AbstractValidator<GetOwnerByIdQuery>
{
    public GetOwnerByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
