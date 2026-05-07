using MediatR;
using VetCare.Domain.Pets;

namespace VetCare.Application.Pets.Commands.UpdatePet;

public sealed record UpdatePetCommand(
    Guid Id,
    string Name,
    Species Species,
    string Breed,
    DateOnly DateOfBirth,
    string? PhotoUrl) : IRequest<PetDto>;
