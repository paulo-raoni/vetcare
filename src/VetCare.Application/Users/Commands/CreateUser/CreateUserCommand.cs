using System.Text.Json.Serialization;
using MediatR;
using VetCare.Application.Abstractions.Messaging;
using VetCare.Domain.Users;

namespace VetCare.Application.Users.Commands.CreateUser;

public sealed record CreateUserCommand(
    string Email,
    [property: JsonIgnore] string Password,
    UserRole Role) : IRequest<UserDto>, ICommand;
