using FluentValidation;
using VetCare.Domain.Appointments;

namespace VetCare.Application.Appointments.Commands.ScheduleAppointment;

public sealed class ScheduleAppointmentCommandValidator : AbstractValidator<ScheduleAppointmentCommand>
{
    public ScheduleAppointmentCommandValidator()
    {
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.VetUserId).NotEmpty();

        RuleFor(x => x.ScheduledAt)
            .Must(d => d > DateTime.UtcNow)
            .WithMessage("ScheduledAt must be in the future.");

        RuleFor(x => x.Notes)
            .MaximumLength(Appointment.NotesMaxLength);
    }
}
