using FluentValidation;

namespace VetCare.Application.Appointments.Commands.CancelAppointment;

public sealed class CancelAppointmentCommandValidator : AbstractValidator<CancelAppointmentCommand>
{
    public CancelAppointmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
