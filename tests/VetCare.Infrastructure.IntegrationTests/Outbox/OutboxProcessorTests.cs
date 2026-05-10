using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using VetCare.Application.Abstractions.Messaging;
using VetCare.Application.Appointments.Events;
using VetCare.Domain.Appointments;
using VetCare.Infrastructure.Outbox;
using VetCare.Infrastructure.Persistence;

namespace VetCare.Infrastructure.IntegrationTests.Outbox;

[Collection(IntegrationTestsCollection.Name)]
public sealed class OutboxProcessorTests : IAsyncLifetime
{
    private readonly VetCareWebApplicationFactory _factory;

    public OutboxProcessorTests(VetCareWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        _factory.ClearSubstitutes();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ProcessOnceAsync_marks_outbox_row_processed_when_publish_succeeds()
    {
        _factory.ClearSubstitutes();

        var rowId = await SeedOutboxRowAsync();
        var processor = _factory.Services.GetRequiredService<OutboxProcessor>();

        await processor.ProcessOnceAsync(default);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VetCareDbContext>();
        var row = await db.OutboxMessages.SingleAsync(o => o.Id == rowId);

        row.ProcessedOnUtc.Should().NotBeNull();
        row.Attempts.Should().Be(0);
        row.Error.Should().BeNull();
    }

    [Fact]
    public async Task ProcessOnceAsync_increments_attempts_and_records_error_when_publish_fails()
    {
        _factory.ClearSubstitutes();
        ConfigureSqsToThrow();

        var rowId = await SeedOutboxRowAsync();
        var processor = _factory.Services.GetRequiredService<OutboxProcessor>();

        await processor.ProcessOnceAsync(default);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VetCareDbContext>();
        var row = await db.OutboxMessages.SingleAsync(o => o.Id == rowId);

        row.ProcessedOnUtc.Should().BeNull();
        row.Attempts.Should().Be(1);
        row.Error.Should().NotBeNullOrEmpty();
        row.NextAttemptOnUtc.Should().NotBeNull();
        row.NextAttemptOnUtc!.Value.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task ProcessOnceAsync_skips_rows_whose_NextAttemptOnUtc_is_in_the_future()
    {
        _factory.ClearSubstitutes();

        var rowId = await SeedOutboxRowAsync(nextAttemptOnUtc: DateTime.UtcNow.AddHours(1));
        var processor = _factory.Services.GetRequiredService<OutboxProcessor>();

        await processor.ProcessOnceAsync(default);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VetCareDbContext>();
        var row = await db.OutboxMessages.SingleAsync(o => o.Id == rowId);

        row.ProcessedOnUtc.Should().BeNull();
        row.Attempts.Should().Be(0);
    }

    [Fact]
    public async Task ProcessOnceAsync_excludes_rows_that_already_reached_max_attempts()
    {
        _factory.ClearSubstitutes();
        ConfigureSqsToThrow();

        var rowId = await SeedOutboxRowAsync(attempts: 10);
        var processor = _factory.Services.GetRequiredService<OutboxProcessor>();

        await processor.ProcessOnceAsync(default);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VetCareDbContext>();
        var row = await db.OutboxMessages.SingleAsync(o => o.Id == rowId);

        row.Attempts.Should().Be(10);
        row.ProcessedOnUtc.Should().BeNull();
    }

    [Fact]
    public async Task ProcessOnceAsync_recovers_after_publish_failure_when_next_attempt_elapses()
    {
        _factory.ClearSubstitutes();
        ConfigureSqsToThrow();

        var rowId = await SeedOutboxRowAsync();
        var processor = _factory.Services.GetRequiredService<OutboxProcessor>();

        await processor.ProcessOnceAsync(default);

        await using (var failureScope = _factory.Services.CreateAsyncScope())
        {
            var failureDb = failureScope.ServiceProvider.GetRequiredService<VetCareDbContext>();
            var failed = await failureDb.OutboxMessages.SingleAsync(o => o.Id == rowId);

            failed.Attempts.Should().Be(1);
            failed.ProcessedOnUtc.Should().BeNull();
            failed.Error.Should().NotBeNullOrEmpty();
            failed.NextAttemptOnUtc.Should().NotBeNull();
            failed.NextAttemptOnUtc!.Value.Should().BeAfter(DateTime.UtcNow);
        }

        _factory.ClearSubstitutes();

        await using (var resetScope = _factory.Services.CreateAsyncScope())
        {
            var resetDb = resetScope.ServiceProvider.GetRequiredService<VetCareDbContext>();
            var row = await resetDb.OutboxMessages.SingleAsync(o => o.Id == rowId);
            row.NextAttemptOnUtc = DateTime.UtcNow.AddSeconds(-1);
            await resetDb.SaveChangesAsync();
        }

        await processor.ProcessOnceAsync(default);

        await using (var successScope = _factory.Services.CreateAsyncScope())
        {
            var successDb = successScope.ServiceProvider.GetRequiredService<VetCareDbContext>();
            var recovered = await successDb.OutboxMessages.SingleAsync(o => o.Id == rowId);

            recovered.ProcessedOnUtc.Should().NotBeNull();
            recovered.Attempts.Should().Be(1);
            recovered.Error.Should().BeNull();
        }
    }

    private async Task<Guid> SeedOutboxRowAsync(
        DateTime? nextAttemptOnUtc = null,
        int attempts = 0)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VetCareDbContext>();

        var tenantId = Guid.NewGuid();
        var domainEvent = new AppointmentScheduledEvent(
            AppointmentId: Guid.NewGuid(),
            TenantId: tenantId,
            PetId: Guid.NewGuid(),
            VetUserId: Guid.NewGuid(),
            ScheduledAt: DateTime.UtcNow.AddDays(1));

        var row = new OutboxMessage
        {
            Id = domainEvent.EventId,
            OccurredOnUtc = domainEvent.OccurredOnUtc,
            Type = domainEvent.GetType().AssemblyQualifiedName!,
            Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            TenantId = tenantId,
            ProcessedOnUtc = null,
            Error = null,
            Attempts = attempts,
            NextAttemptOnUtc = nextAttemptOnUtc,
        };

        db.OutboxMessages.Add(row);
        await db.SaveChangesAsync();
        return row.Id;
    }

    private void ConfigureSqsToThrow()
    {
        _factory.SqsPublisher
            .PublishAsync(
                Arg.Any<string>(),
                Arg.Any<AppointmentReminderMessage>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Simulated SQS failure"));
    }
}
