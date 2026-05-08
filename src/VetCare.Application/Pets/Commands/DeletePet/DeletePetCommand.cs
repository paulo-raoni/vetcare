using MediatR;
using VetCare.Application.Abstractions.Messaging;

namespace VetCare.Application.Pets.Commands.DeletePet;

public sealed record DeletePetCommand(Guid Id) : IRequest, ICommand;
