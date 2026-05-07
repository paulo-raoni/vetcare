using MediatR;

namespace VetCare.Application.Owners.Commands.DeleteOwner;

public sealed record DeleteOwnerCommand(Guid Id) : IRequest;
