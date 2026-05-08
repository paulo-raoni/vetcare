using FluentValidation;

namespace VetCare.Application.Appointments.Commands.ConfirmAppointment;

public sealed class ConfirmAppointmentCommandValidator : AbstractValidator<ConfirmAppointmentCommand>
{
    public ConfirmAppointmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
