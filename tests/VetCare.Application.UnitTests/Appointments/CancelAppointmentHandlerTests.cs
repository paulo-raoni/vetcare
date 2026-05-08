using FluentAssertions;
using MapsterMapper;
using NSubstitute;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Abstractions.Specifications;
using VetCare.Application.Appointments;
using VetCare.Application.Appointments.Commands.CancelAppointment;
using VetCare.Application.Common.Exceptions;
using VetCare.Domain.Appointments;
using VetCare.Domain.Primitives;

namespace VetCare.Application.UnitTests.Appointments;

public sealed class CancelAppointmentHandlerTests
{
    private readonly IRepository<Appointment> _appointments = Substitute.For<IRepository<Appointment>>();
    private readonly IVetCareDbContext _db = Substitute.For<IVetCareDbContext>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();

    public CancelAppointmentHandlerTests()
    {
        _mapper.Map<AppointmentDto>(Arg.Any<Appointment>())
            .Returns(ci =>
            {
                var a = ci.Arg<Appointment>();
                return new AppointmentDto(a.Id, a.TenantId, a.PetId, a.VetUserId, a.ScheduledAt, a.Status, a.Notes, a.CreatedAt, a.UpdatedAt);
            });
    }

    [Fact]
    public async Task Handle_cancels_scheduled_appointment()
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1));
        _appointments.SingleOrDefaultAsync(Arg.Any<ISpecification<Appointment>>(), Arg.Any<CancellationToken>())
            .Returns(appointment);

        var handler = new CancelAppointmentCommandHandler(_appointments, _db, _mapper);

        var result = await handler.Handle(new CancelAppointmentCommand(appointment.Id), CancellationToken.None);

        result.Status.Should().Be(AppointmentStatus.Cancelled);
        appointment.DomainEvents.OfType<AppointmentCancelledEvent>().Should().HaveCount(1);
        await _db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_DomainException_for_invalid_transition_when_already_cancelled()
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1));
        appointment.Cancel();
        _appointments.SingleOrDefaultAsync(Arg.Any<ISpecification<Appointment>>(), Arg.Any<CancellationToken>())
            .Returns(appointment);

        var handler = new CancelAppointmentCommandHandler(_appointments, _db, _mapper);

        var act = async () => await handler.Handle(new CancelAppointmentCommand(appointment.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_throws_NotFound_when_appointment_missing()
    {
        _appointments.SingleOrDefaultAsync(Arg.Any<ISpecification<Appointment>>(), Arg.Any<CancellationToken>())
            .Returns((Appointment?)null);

        var handler = new CancelAppointmentCommandHandler(_appointments, _db, _mapper);

        var act = async () => await handler.Handle(new CancelAppointmentCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
