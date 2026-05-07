using MediatR;
using VetCare.Domain.Pets;

namespace VetCare.Application.Pets.Commands.CreatePet;

public sealed record CreatePetCommand(
    Guid OwnerId,
    string Name,
    Species Species,
    string Breed,
    DateOnly DateOfBirth,
    string? PhotoUrl) : IRequest<PetDto>;
