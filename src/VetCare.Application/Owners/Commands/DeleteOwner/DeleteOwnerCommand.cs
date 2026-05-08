using MediatR;
using VetCare.Application.Abstractions.Messaging;

namespace VetCare.Application.Owners.Commands.DeleteOwner;

public sealed record DeleteOwnerCommand(Guid Id) : IRequest, ICommand;
