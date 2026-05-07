using MediatR;

namespace VetCare.Application.Pets.Commands.DeletePet;

public sealed record DeletePetCommand(Guid Id) : IRequest;
