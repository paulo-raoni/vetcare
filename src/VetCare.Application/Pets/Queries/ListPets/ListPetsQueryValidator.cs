using FluentValidation;

namespace VetCare.Application.Pets.Queries.ListPets;

public sealed class ListPetsQueryValidator : AbstractValidator<ListPetsQuery>
{
    public ListPetsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
