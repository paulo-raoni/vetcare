using FluentValidation;

namespace VetCare.Application.Owners.Queries.ListOwners;

public sealed class ListOwnersQueryValidator : AbstractValidator<ListOwnersQuery>
{
    public ListOwnersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
