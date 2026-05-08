using FluentValidation;

namespace VetCare.Application.Appointments.Commands.CompleteAppointment;

public sealed class CompleteAppointmentCommandValidator : AbstractValidator<CompleteAppointmentCommand>
{
    public CompleteAppointmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
