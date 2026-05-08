using FluentAssertions;
using MapsterMapper;
using NSubstitute;
using VetCare.Application.Abstractions.MultiTenancy;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Abstractions.Specifications;
using VetCare.Application.Appointments;
using VetCare.Application.Appointments.Commands.ScheduleAppointment;
using VetCare.Application.Common.Exceptions;
using VetCare.Domain.Appointments;
using VetCare.Domain.Pets;

namespace VetCare.Application.UnitTests.Appointments;

public sealed class ScheduleAppointmentHandlerTests
{
    private readonly IRepository<Appointment> _appointments = Substitute.For<IRepository<Appointment>>();
    private readonly IRepository<Pet> _pets = Substitute.For<IRepository<Pet>>();
    private readonly IVetCareDbContext _db = Substitute.For<IVetCareDbContext>();
    private readonly ITenantProvider _tenant = Substitute.For<ITenantProvider>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly Guid _tenantId = Guid.NewGuid();

    public ScheduleAppointmentHandlerTests()
    {
        _tenant.HasTenant.Returns(true);
        _tenant.TenantId.Returns(_tenantId);
        _mapper.Map<AppointmentDto>(Arg.Any<Appointment>())
            .Returns(ci =>
            {
                var a = ci.Arg<Appointment>();
                return new AppointmentDto(a.Id, a.TenantId, a.PetId, a.VetUserId, a.ScheduledAt, a.Status, a.Notes, a.CreatedAt, a.UpdatedAt);
            });
    }

    [Fact]
    public async Task Handle_schedules_appointment_and_raises_event()
    {
        var pet = new Pet(_tenantId, Guid.NewGuid(), "Rex", Species.Dog, "Lab", new DateOnly(2022, 1, 1));
        _pets.SingleOrDefaultAsync(Arg.Any<ISpecification<Pet>>(), Arg.Any<CancellationToken>())
            .Returns(pet);

        var when = DateTime.UtcNow.AddDays(2);
        var handler = new ScheduleAppointmentCommandHandler(_appointments, _pets, _db, _tenant, _mapper);

        var result = await handler.Handle(
            new ScheduleAppointmentCommand(pet.Id, Guid.NewGuid(), when, "checkup"),
            CancellationToken.None);

        result.PetId.Should().Be(pet.Id);
        result.TenantId.Should().Be(_tenantId);
        result.Status.Should().Be(AppointmentStatus.Scheduled);

        _appointments.Received(1).Add(Arg.Is<Appointment>(a =>
            a.PetId == pet.Id
            && a.TenantId == _tenantId
            && a.DomainEvents.OfType<AppointmentScheduledEvent>().Any()));
        await _db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_NotFound_when_pet_missing()
    {
        _pets.SingleOrDefaultAsync(Arg.Any<ISpecification<Pet>>(), Arg.Any<CancellationToken>())
            .Returns((Pet?)null);

        var handler = new ScheduleAppointmentCommandHandler(_appointments, _pets, _db, _tenant, _mapper);

        var act = async () => await handler.Handle(
            new ScheduleAppointmentCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), null),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _appointments.DidNotReceive().Add(Arg.Any<Appointment>());
    }

    [Fact]
    public void Validator_rejects_past_scheduled_at()
    {
        var validator = new ScheduleAppointmentCommandValidator();

        var result = validator.Validate(new ScheduleAppointmentCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-1), null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ScheduleAppointmentCommand.ScheduledAt));
    }

    [Fact]
    public void Validator_accepts_future_scheduled_at()
    {
        var validator = new ScheduleAppointmentCommandValidator();

        var result = validator.Validate(new ScheduleAppointmentCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), null));

        result.IsValid.Should().BeTrue();
    }
}
