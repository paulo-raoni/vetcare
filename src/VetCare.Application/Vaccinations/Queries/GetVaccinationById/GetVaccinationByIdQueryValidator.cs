using FluentValidation;

namespace VetCare.Application.Vaccinations.Queries.GetVaccinationById;

public sealed class GetVaccinationByIdQueryValidator : AbstractValidator<GetVaccinationByIdQuery>
{
    public GetVaccinationByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
