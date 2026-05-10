using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VetCare.Domain.Primitives;
using VetCare.Infrastructure.MultiTenancy;
using VetCare.Infrastructure.Persistence;

namespace VetCare.Infrastructure.Outbox;

public sealed class OutboxProcessor : BackgroundService
{
    private const int ErrorTextMaxLength = 4000;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxOptions> options,
        ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox processor batch iteration failed");
            }

            try
            {
                await Task.Delay(_options.PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public async Task ProcessOnceAsync(CancellationToken ct)
    {
        await using var batchScope = _scopeFactory.CreateAsyncScope();
        var batchDb = batchScope.ServiceProvider.GetRequiredService<VetCareDbContext>();

        await using var transaction = await batchDb.Database.BeginTransactionAsync(ct);

        var maxAttempts = _options.MaxAttempts;
        var batchSize = _options.BatchSize;

        var batch = await batchDb.OutboxMessages
            .FromSqlInterpolated($"""
                SELECT * FROM vetcare.outbox_messages
                WHERE "ProcessedOnUtc" IS NULL
                  AND "Attempts" < {maxAttempts}
                  AND ("NextAttemptOnUtc" IS NULL OR "NextAttemptOnUtc" <= NOW())
                ORDER BY "OccurredOnUtc"
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(ct);

        if (batch.Count == 0)
        {
            await transaction.CommitAsync(ct);
            return;
        }

        foreach (var message in batch)
        {
            await ProcessOneAsync(message, ct);
        }

        await batchDb.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private async Task ProcessOneAsync(OutboxMessage message, CancellationToken ct)
    {
        await using var msgScope = _scopeFactory.CreateAsyncScope();
        var publisher = msgScope.ServiceProvider.GetRequiredService<IPublisher>();
        var tenantProvider = msgScope.ServiceProvider.GetRequiredService<CurrentTenantProvider>();

        try
        {
            var eventType = Type.GetType(message.Type);
            if (eventType is null)
            {
                MarkFailure(message, $"Could not resolve event type: {message.Type}");
                return;
            }

            if (JsonSerializer.Deserialize(message.Content, eventType) is not IDomainEvent domainEvent)
            {
                MarkFailure(message, "Failed to deserialize event content");
                return;
            }

            tenantProvider.SetTenant(message.TenantId);

            await publisher.Publish(domainEvent, ct);

            message.ProcessedOnUtc = DateTime.UtcNow;
            message.Error = null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            MarkFailure(message, Truncate(ex.ToString(), ErrorTextMaxLength));
        }
    }

    private void MarkFailure(OutboxMessage message, string error)
    {
        message.Attempts++;
        message.Error = error;

        if (message.Attempts >= _options.MaxAttempts)
        {
            _logger.LogWarning(
                "Outbox message {MessageId} ({Type}) reached max attempts ({MaxAttempts}). Last error: {Error}",
                message.Id, message.Type, _options.MaxAttempts, error);
            return;
        }

        message.NextAttemptOnUtc = DateTime.UtcNow + ComputeBackoff(message.Attempts);
    }

    private TimeSpan ComputeBackoff(int attempts)
    {
        var seconds = Math.Min(
            _options.BaseRetryDelay.TotalSeconds * Math.Pow(2, attempts - 1),
            _options.MaxRetryDelay.TotalSeconds);
        return TimeSpan.FromSeconds(seconds);
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
