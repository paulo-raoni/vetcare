namespace VetCare.Application.Abstractions.Messaging;

public interface ISqsPublisher
{
    Task PublishAsync<T>(string queueName, T message, CancellationToken cancellationToken = default)
        where T : class;
}
