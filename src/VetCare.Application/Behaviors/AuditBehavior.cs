using MediatR;
using Microsoft.Extensions.Logging;
using VetCare.Application.Abstractions.Auditing;
using VetCare.Application.Abstractions.Identity;
using VetCare.Application.Abstractions.Messaging;
using VetCare.Application.Auditing;

namespace VetCare.Application.Behaviors;

public sealed class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IAuditRepository _audit;
    private readonly ICurrentUserService _user;
    private readonly ILogger<AuditBehavior<TRequest, TResponse>> _logger;

    public AuditBehavior(
        IAuditRepository audit,
        ICurrentUserService user,
        ILogger<AuditBehavior<TRequest, TResponse>> logger)
    {
        _audit = audit;
        _user = user;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is not ICommand)
        {
            return response;
        }

        var entry = new AuditEntry(
            Id: Guid.NewGuid(),
            TenantId: _user.TenantId,
            UserId: _user.UserId,
            Action: typeof(TRequest).Name,
            EntityType: null,
            EntityId: null,
            Payload: request,
            OccurredAt: DateTime.UtcNow);

        try
        {
            await _audit.SaveAsync(entry, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist audit entry for {Action}", entry.Action);
        }

        return response;
    }
}
