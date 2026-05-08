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
using VetCare.Application.Pets;
using VetCare.Application.Pets.Commands.CreatePet;
using VetCare.Application.Pets.Commands.DeletePet;
using VetCare.Application.Pets.Commands.UpdatePet;
using VetCare.Application.Pets.Commands.UploadPetPhoto;
using VetCare.Application.Pets.Queries.GetPetById;
using VetCare.Application.Pets.Queries.ListPets;
using VetCare.Domain.Pets;

namespace VetCare.Api.Endpoints.Pets;

internal static class PetEndpoints
{
    public static IEndpointRouteBuilder MapPetEndpoints(this IEndpointRouteBuilder app, ApiVersionSet versionSet)
    {
        var group = app
            .MapGroup("/api/v{version:apiVersion}/pets")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(new ApiVersion(1))
            .RequireAuthorization()
            .WithTags("Pets");

        group.MapGet("/", ListPets)
            .WithName("ListPets")
            .RequireAuthorization(AuthorizationPolicies.AnyStaff)
            .Produces<PagedResult<PetDto>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .WithOpenApi(op =>
            {
                op.Summary = "List pets (paginated, filterable by ownerId)";
                op.Description = "Returns a paginated list of pets scoped to the caller's tenant. Optionally filter by ownerId.";
                return op;
            });

        group.MapGet("/{id:guid}", GetPetById)
            .WithName("GetPetById")
            .RequireAuthorization(AuthorizationPolicies.AnyStaff)
            .Produces<PetDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi(op =>
            {
                op.Summary = "Get a pet by id";
                op.Responses["200"].Content["application/json"].Example = ExamplePet();
                return op;
            });

        group.MapPost("/", CreatePet)
            .WithName("CreatePet")
            .RequireAuthorization(AuthorizationPolicies.VetOrAdmin)
            .Produces<PetDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi(op =>
            {
                op.Summary = "Create a new pet";
                op.RequestBody.Content["application/json"].Example = ExampleCreatePet();
                op.Responses["201"].Content["application/json"].Example = ExamplePet();
                return op;
            });

        group.MapPut("/{id:guid}", UpdatePet)
            .WithName("UpdatePet")
            .RequireAuthorization(AuthorizationPolicies.VetOrAdmin)
            .Produces<PetDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi(op =>
            {
                op.Summary = "Update a pet";
                op.RequestBody.Content["application/json"].Example = ExampleUpdatePet();
                return op;
            });

        group.MapDelete("/{id:guid}", DeletePet)
            .WithName("DeletePet")
            .RequireAuthorization(AuthorizationPolicies.VetOrAdmin)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi(op =>
            {
                op.Summary = "Delete a pet";
                return op;
            });

        group.MapPost("/{id:guid}/photo", UploadPetPhoto)
            .WithName("UploadPetPhoto")
            .RequireAuthorization(AuthorizationPolicies.AnyStaff)
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<PetDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi(op =>
            {
                op.Summary = "Upload a pet photo";
                op.Description = "Accepts multipart/form-data with a single 'file' field. Max size: 5MB. Allowed types: image/jpeg, image/png.";
                return op;
            });

        return app;
    }

    private const long MaxPhotoBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedPhotoContentTypes =
    {
        "image/jpeg",
        "image/png",
    };

    private static readonly byte[] JpegMagic = { 0xFF, 0xD8, 0xFF };

    private static readonly byte[] PngMagic = { 0x89, 0x50, 0x4E, 0x47 };

    internal sealed record CreatePetRequest(
        Guid OwnerId,
        string Name,
        Species Species,
        string Breed,
        DateOnly DateOfBirth,
        string? PhotoUrl);

    internal sealed record UpdatePetRequest(
        string Name,
        Species Species,
        string Breed,
        DateOnly DateOfBirth,
        string? PhotoUrl);

    private static async Task<Ok<PagedResult<PetDto>>> ListPets(
        ISender sender,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? ownerId = null)
    {
        var result = await sender.Send(new ListPetsQuery(page, pageSize, ownerId), cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<PetDto>> GetPetById(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPetByIdQuery(id), cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Created<PetDto>> CreatePet(
        CreatePetRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreatePetCommand(
                request.OwnerId,
                request.Name,
                request.Species,
                request.Breed,
                request.DateOfBirth,
                request.PhotoUrl),
            cancellationToken);
        return TypedResults.Created($"/api/v1/pets/{result.Id}", result);
    }

    private static async Task<Ok<PetDto>> UpdatePet(
        Guid id,
        UpdatePetRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdatePetCommand(
                id,
                request.Name,
                request.Species,
                request.Breed,
                request.DateOfBirth,
                request.PhotoUrl),
            cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<NoContent> DeletePet(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await sender.Send(new DeletePetCommand(id), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<PetDto>, ValidationProblem>> UploadPetPhoto(
        Guid id,
        IFormFile file,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (file is null || file.Length == 0)
        {
            errors["file"] = new[] { "A non-empty file is required." };
            return TypedResults.ValidationProblem(errors);
        }

        if (file.Length > MaxPhotoBytes)
        {
            errors["file"] = new[] { $"File exceeds maximum allowed size of {MaxPhotoBytes} bytes." };
            return TypedResults.ValidationProblem(errors);
        }

        if (!AllowedPhotoContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            errors["file"] = new[] { "Only image/jpeg or image/png content types are allowed." };
            return TypedResults.ValidationProblem(errors);
        }

        await using var stream = file.OpenReadStream();

        if (!await IsAllowedImageMagicAsync(stream, cancellationToken))
        {
            errors["file"] = new[] { "Invalid image file." };
            return TypedResults.ValidationProblem(errors);
        }

        var result = await sender.Send(
            new UploadPetPhotoCommand(id, file.FileName, stream, file.ContentType),
            cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<bool> IsAllowedImageMagicAsync(Stream stream, CancellationToken cancellationToken)
    {
        var header = new byte[4];
        var read = await stream.ReadAsync(header.AsMemory(0, 4), cancellationToken);
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        if (read < 3)
        {
            return false;
        }

        if (header[0] == JpegMagic[0] && header[1] == JpegMagic[1] && header[2] == JpegMagic[2])
        {
            return true;
        }

        if (read >= 4
            && header[0] == PngMagic[0]
            && header[1] == PngMagic[1]
            && header[2] == PngMagic[2]
            && header[3] == PngMagic[3])
        {
            return true;
        }

        return false;
    }

    private static OpenApiObject ExampleCreatePet() => new()
    {
        ["ownerId"] = new OpenApiString("3b6c2d34-2c30-46e3-9c76-5e9e6e2c1f0d"),
        ["name"] = new OpenApiString("Rex"),
        ["species"] = new OpenApiInteger(1),
        ["breed"] = new OpenApiString("Labrador"),
        ["dateOfBirth"] = new OpenApiString("2022-04-15"),
        ["photoUrl"] = new OpenApiString("https://cdn.vetcare.dev/pets/rex.jpg"),
    };

    private static OpenApiObject ExampleUpdatePet() => new()
    {
        ["name"] = new OpenApiString("Rex"),
        ["species"] = new OpenApiInteger(1),
        ["breed"] = new OpenApiString("Labrador Retriever"),
        ["dateOfBirth"] = new OpenApiString("2022-04-15"),
        ["photoUrl"] = new OpenApiString("https://cdn.vetcare.dev/pets/rex-updated.jpg"),
    };

    private static OpenApiObject ExamplePet() => new()
    {
        ["id"] = new OpenApiString("c97e2d8a-cf9c-4b31-9e15-7a2eb1f8a3c4"),
        ["tenantId"] = new OpenApiString("a4f8e6c5-9d2a-4f86-9c11-2b3a82a1d9e2"),
        ["ownerId"] = new OpenApiString("3b6c2d34-2c30-46e3-9c76-5e9e6e2c1f0d"),
        ["name"] = new OpenApiString("Rex"),
        ["species"] = new OpenApiInteger(1),
        ["breed"] = new OpenApiString("Labrador"),
        ["dateOfBirth"] = new OpenApiString("2022-04-15"),
        ["photoUrl"] = new OpenApiString("https://cdn.vetcare.dev/pets/rex.jpg"),
        ["createdAt"] = new OpenApiString("2026-05-07T12:00:00Z"),
        ["updatedAt"] = new OpenApiString("2026-05-07T12:00:00Z"),
    };
}
