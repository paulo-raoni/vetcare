using FluentValidation;

namespace VetCare.Application.Appointments.Queries.ListAppointments;

public sealed class ListAppointmentsQueryValidator : AbstractValidator<ListAppointmentsQuery>
{
    public ListAppointmentsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);

        RuleFor(x => x)
            .Must(q => q.From is null || q.To is null || q.From <= q.To)
            .WithMessage("'From' must be on or before 'To'.");
    }
}
