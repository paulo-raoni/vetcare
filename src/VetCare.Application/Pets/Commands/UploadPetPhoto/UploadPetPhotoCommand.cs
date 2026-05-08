using System.Text.Json.Serialization;
using MediatR;
using VetCare.Application.Abstractions.Messaging;

namespace VetCare.Application.Pets.Commands.UploadPetPhoto;

public sealed record UploadPetPhotoCommand(
    Guid PetId,
    string FileName,
    [property: JsonIgnore] Stream Content,
    string ContentType) : IRequest<PetDto>, ICommand;
