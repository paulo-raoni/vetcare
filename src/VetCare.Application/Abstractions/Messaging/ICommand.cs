namespace VetCare.Application.Abstractions.Messaging;

/// <summary>
/// Marker interface for state-mutating MediatR requests. Used by the audit
/// pipeline behavior to distinguish commands (audited) from queries (skipped).
/// </summary>
public interface ICommand
{
}
