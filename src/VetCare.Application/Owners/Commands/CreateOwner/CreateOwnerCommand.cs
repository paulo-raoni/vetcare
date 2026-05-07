using MediatR;

namespace VetCare.Application.Owners.Commands.CreateOwner;

public sealed record CreateOwnerCommand(
    string FullName,
    string Phone,
    string Email) : IRequest<OwnerDto>;
