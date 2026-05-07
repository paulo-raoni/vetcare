using FluentValidation;

namespace VetCare.Application.Pets.Queries.GetPetById;

public sealed class GetPetByIdQueryValidator : AbstractValidator<GetPetByIdQuery>
{
    public GetPetByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
