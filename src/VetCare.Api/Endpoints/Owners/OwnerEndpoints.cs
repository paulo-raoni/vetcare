using Asp.Versioning;
using Asp.Versioning.Builder;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using VetCare.Api.Authorization;
using VetCare.Application.Common.Pagination;
using VetCare.Application.Owners;
using VetCare.Application.Owners.Commands.CreateOwner;
using VetCare.Application.Owners.Commands.DeleteOwner;
using VetCare.Application.Owners.Commands.UpdateOwner;
using VetCare.Application.Owners.Queries.GetOwnerById;
using VetCare.Application.Owners.Queries.ListOwners;

namespace VetCare.Api.Endpoints.Owners;

internal static class OwnerEndpoints
{
    public static IEndpointRouteBuilder MapOwnerEndpoints(this IEndpointRouteBuilder app, ApiVersionSet versionSet)
    {
        var group = app
            .MapGroup("/api/v{version:apiVersion}/owners")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(new ApiVersion(1))
            .RequireAuthorization()
            .WithTags("Owners");

        group.MapGet("/", ListOwners)
            .WithName("ListOwners")
            .RequireAuthorization(AuthorizationPolicies.AnyStaff)
            .Produces<PagedResult<OwnerDto>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .WithOpenApi(op =>
            {
                op.Summary = "List owners (paginated)";
                op.Description = "Returns a paginated list of owners scoped to the caller's tenant.";
                return op;
            });

        group.MapGet("/{id:guid}", GetOwnerById)
            .WithName("GetOwnerById")
            .RequireAuthorization(AuthorizationPolicies.AnyStaff)
            .Produces<OwnerDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi(op =>
            {
                op.Summary = "Get an owner by id";
                op.Responses["200"].Content["application/json"].Example = ExampleOwner();
                return op;
            });

        group.MapPost("/", CreateOwner)
            .WithName("CreateOwner")
            .RequireAuthorization(AuthorizationPolicies.VetOrAdmin)
            .Produces<OwnerDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .WithOpenApi(op =>
            {
                op.Summary = "Create a new owner";
                op.RequestBody.Content["application/json"].Example = ExampleCreateOwner();
                op.Responses["201"].Content["application/json"].Example = ExampleOwner();
                return op;
            });

        group.MapPut("/{id:guid}", UpdateOwner)
            .WithName("UpdateOwner")
            .RequireAuthorization(AuthorizationPolicies.VetOrAdmin)
            .Produces<OwnerDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi(op =>
            {
                op.Summary = "Update an owner";
                op.RequestBody.Content["application/json"].Example = ExampleUpdateOwner();
                return op;
            });

        group.MapDelete("/{id:guid}", DeleteOwner)
            .WithName("DeleteOwner")
            .RequireAuthorization(AuthorizationPolicies.VetOrAdmin)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi(op =>
            {
                op.Summary = "Delete an owner";
                return op;
            });

        return app;
    }

    internal sealed record CreateOwnerRequest(string FullName, string Phone, string Email);

    internal sealed record UpdateOwnerRequest(string FullName, string Phone, string Email);

    private static async Task<Ok<PagedResult<OwnerDto>>> ListOwners(
        ISender sender,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await sender.Send(new ListOwnersQuery(page, pageSize), cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<OwnerDto>> GetOwnerById(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetOwnerByIdQuery(id), cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Created<OwnerDto>> CreateOwner(
        CreateOwnerRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateOwnerCommand(request.FullName, request.Phone, request.Email),
            cancellationToken);
        return TypedResults.Created($"/api/v1/owners/{result.Id}", result);
    }

    private static async Task<Ok<OwnerDto>> UpdateOwner(
        Guid id,
        UpdateOwnerRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateOwnerCommand(id, request.FullName, request.Phone, request.Email),
            cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<NoContent> DeleteOwner(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteOwnerCommand(id), cancellationToken);
        return TypedResults.NoContent();
    }

    private static OpenApiObject ExampleCreateOwner() => new()
    {
        ["fullName"] = new OpenApiString("Jane Doe"),
        ["phone"] = new OpenApiString("+5511999999999"),
        ["email"] = new OpenApiString("jane@example.com"),
    };

    private static OpenApiObject ExampleUpdateOwner() => new()
    {
        ["fullName"] = new OpenApiString("Jane Doe Smith"),
        ["phone"] = new OpenApiString("+5511988887777"),
        ["email"] = new OpenApiString("jane.smith@example.com"),
    };

    private static OpenApiObject ExampleOwner() => new()
    {
        ["id"] = new OpenApiString("3b6c2d34-2c30-46e3-9c76-5e9e6e2c1f0d"),
        ["tenantId"] = new OpenApiString("a4f8e6c5-9d2a-4f86-9c11-2b3a82a1d9e2"),
        ["fullName"] = new OpenApiString("Jane Doe"),
        ["phone"] = new OpenApiString("+5511999999999"),
        ["email"] = new OpenApiString("jane@example.com"),
        ["createdAt"] = new OpenApiString("2026-05-07T12:00:00Z"),
        ["updatedAt"] = new OpenApiString("2026-05-07T12:00:00Z"),
    };
}
