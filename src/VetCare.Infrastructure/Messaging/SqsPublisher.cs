using System.Collections.Concurrent;
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using VetCare.Application.Abstractions.Messaging;

namespace VetCare.Infrastructure.Messaging;

public sealed class SqsPublisher : ISqsPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IAmazonSQS _sqs;
    private readonly ILogger<SqsPublisher> _logger;
    private readonly ConcurrentDictionary<string, string> _queueUrlCache = new(StringComparer.Ordinal);

    public SqsPublisher(IAmazonSQS sqs, ILogger<SqsPublisher> logger)
    {
        _sqs = sqs;
        _logger = logger;
    }

    public async Task PublishAsync<T>(string queueName, T message, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentNullException.ThrowIfNull(message);

        var queueUrl = await ResolveQueueUrlAsync(queueName, cancellationToken);
        var body = JsonSerializer.Serialize(message, SerializerOptions);

        await _sqs.SendMessageAsync(
            new SendMessageRequest { QueueUrl = queueUrl, MessageBody = body },
            cancellationToken);

        _logger.LogInformation("Published SQS message to {QueueName} ({QueueUrl})", queueName, queueUrl);
    }

    private async Task<string> ResolveQueueUrlAsync(string queueName, CancellationToken cancellationToken)
    {
        if (_queueUrlCache.TryGetValue(queueName, out var cached))
        {
            return cached;
        }

        var response = await _sqs.GetQueueUrlAsync(new GetQueueUrlRequest { QueueName = queueName }, cancellationToken);
        _queueUrlCache[queueName] = response.QueueUrl;
        return response.QueueUrl;
    }
}
