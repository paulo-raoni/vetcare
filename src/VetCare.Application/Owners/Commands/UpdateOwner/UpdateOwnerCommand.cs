using MediatR;
using VetCare.Application.Abstractions.Messaging;

namespace VetCare.Application.Owners.Commands.UpdateOwner;

public sealed record UpdateOwnerCommand(
    Guid Id,
    string FullName,
    string Phone,
    string Email) : IRequest<OwnerDto>, ICommand;
