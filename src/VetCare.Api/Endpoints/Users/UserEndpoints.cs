using Asp.Versioning;
using Asp.Versioning.Builder;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using VetCare.Api.Authorization;
using VetCare.Application.Users;
using VetCare.Application.Users.Commands.CreateUser;
using VetCare.Domain.Users;

namespace VetCare.Api.Endpoints.Users;

internal static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app, ApiVersionSet versionSet)
    {
        var group = app
            .MapGroup("/api/v{version:apiVersion}/users")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(new ApiVersion(1))
            .RequireAuthorization()
            .WithTags("Users");

        group.MapPost("/", CreateUser)
            .WithName("CreateUser")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .Produces<UserDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithOpenApi(op =>
            {
                op.Summary = "Create a user within the caller's tenant (admin only)";
                op.RequestBody.Content["application/json"].Example = ExampleCreateUser();
                op.Responses["201"].Content["application/json"].Example = ExampleUser();
                return op;
            });

        return app;
    }

    internal sealed record CreateUserRequest(string Email, string Password, UserRole Role);

    private static async Task<IResult> CreateUser(
        CreateUserRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(
                new CreateUserCommand(request.Email, request.Password, request.Role),
                cancellationToken);
            return TypedResults.Created($"/api/v1/users/{result.Id}", result);
        }
        catch (EmailAlreadyInUseException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }

    private static OpenApiObject ExampleCreateUser() => new()
    {
        ["email"] = new OpenApiString("vet@acme.test"),
        ["password"] = new OpenApiString("ChangeMe123!"),
        ["role"] = new OpenApiInteger((int)UserRole.Vet),
    };

    private static OpenApiObject ExampleUser() => new()
    {
        ["id"] = new OpenApiString("3b6c2d34-2c30-46e3-9c76-5e9e6e2c1f0d"),
        ["tenantId"] = new OpenApiString("a4f8e6c5-9d2a-4f86-9c11-2b3a82a1d9e2"),
        ["email"] = new OpenApiString("vet@acme.test"),
        ["role"] = new OpenApiInteger((int)UserRole.Vet),
        ["createdAt"] = new OpenApiString("2026-05-09T12:00:00Z"),
        ["updatedAt"] = new OpenApiString("2026-05-09T12:00:00Z"),
    };
}
