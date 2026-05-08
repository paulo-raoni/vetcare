using FluentValidation;

namespace VetCare.Application.Appointments.Queries.GetAppointmentById;

public sealed class GetAppointmentByIdQueryValidator : AbstractValidator<GetAppointmentByIdQuery>
{
    public GetAppointmentByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
