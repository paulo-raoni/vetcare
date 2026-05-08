using FluentValidation;

namespace VetCare.Application.Vaccinations.Queries.ListVaccinations;

public sealed class ListVaccinationsQueryValidator : AbstractValidator<ListVaccinationsQuery>
{
    public ListVaccinationsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
