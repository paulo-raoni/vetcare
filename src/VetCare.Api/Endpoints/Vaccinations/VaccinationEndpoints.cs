using Asp.Versioning;
using Asp.Versioning.Builder;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using VetCare.Application.Common.Pagination;
using VetCare.Application.Vaccinations;
using VetCare.Application.Vaccinations.Commands.RecordVaccination;
using VetCare.Application.Vaccinations.Commands.UpdateVaccination;
using VetCare.Application.Vaccinations.Queries.GetVaccinationById;
using VetCare.Application.Vaccinations.Queries.ListVaccinations;

namespace VetCare.Api.Endpoints.Vaccinations;

internal static class VaccinationEndpoints
{
    public static IEndpointRouteBuilder MapVaccinationEndpoints(this IEndpointRouteBuilder app, ApiVersionSet versionSet)
    {
        var group = app
            .MapGroup("/api/v{version:apiVersion}/vaccinations")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(new ApiVersion(1))
            .RequireAuthorization()
            .WithTags("Vaccinations");

        group.MapGet("/", ListVaccinations)
            .WithName("ListVaccinations")
            .Produces<PagedResult<VaccinationDto>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .WithOpenApi(op =>
            {
                op.Summary = "List vaccinations (paginated, filterable by petId)";
                op.Description = "Returns a paginated list of vaccinations scoped to the caller's tenant. Optionally filter by petId.";
                return op;
            });

        group.MapGet("/{id:guid}", GetVaccinationById)
            .WithName("GetVaccinationById")
            .Produces<VaccinationDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi(op =>
            {
                op.Summary = "Get a vaccination by id";
                op.Responses["200"].Content["application/json"].Example = ExampleVaccination();
                return op;
            });

        group.MapPost("/", RecordVaccination)
            .WithName("RecordVaccination")
            .Produces<VaccinationDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi(op =>
            {
                op.Summary = "Record a new vaccination";
                op.RequestBody.Content["application/json"].Example = ExampleRecordVaccination();
                op.Responses["201"].Content["application/json"].Example = ExampleVaccination();
                return op;
            });

        group.MapPut("/{id:guid}", UpdateVaccination)
            .WithName("UpdateVaccination")
            .Produces<VaccinationDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi(op =>
            {
                op.Summary = "Update a vaccination record";
                op.RequestBody.Content["application/json"].Example = ExampleUpdateVaccination();
                return op;
            });

        return app;
    }

    internal sealed record RecordVaccinationRequest(
        Guid PetId,
        string VaccineName,
        DateTime AdministeredAt,
        DateTime? NextDueAt,
        string BatchNumber);

    internal sealed record UpdateVaccinationRequest(
        string VaccineName,
        DateTime AdministeredAt,
        DateTime? NextDueAt,
        string BatchNumber);

    private static async Task<Ok<PagedResult<VaccinationDto>>> ListVaccinations(
        ISender sender,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? petId = null)
    {
        var result = await sender.Send(new ListVaccinationsQuery(page, pageSize, petId), cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<VaccinationDto>> GetVaccinationById(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetVaccinationByIdQuery(id), cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Created<VaccinationDto>> RecordVaccination(
        RecordVaccinationRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RecordVaccinationCommand(
                request.PetId,
                request.VaccineName,
                request.AdministeredAt,
                request.NextDueAt,
                request.BatchNumber),
            cancellationToken);
        return TypedResults.Created($"/api/v1/vaccinations/{result.Id}", result);
    }

    private static async Task<Ok<VaccinationDto>> UpdateVaccination(
        Guid id,
        UpdateVaccinationRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateVaccinationCommand(
                id,
                request.VaccineName,
                request.AdministeredAt,
                request.NextDueAt,
                request.BatchNumber),
            cancellationToken);
        return TypedResults.Ok(result);
    }

    private static OpenApiObject ExampleRecordVaccination() => new()
    {
        ["petId"] = new OpenApiString("c97e2d8a-cf9c-4b31-9e15-7a2eb1f8a3c4"),
        ["vaccineName"] = new OpenApiString("Rabies"),
        ["administeredAt"] = new OpenApiString("2026-04-01T09:00:00Z"),
        ["nextDueAt"] = new OpenApiString("2027-04-01T09:00:00Z"),
        ["batchNumber"] = new OpenApiString("RAB-2026-0412"),
    };

    private static OpenApiObject ExampleUpdateVaccination() => new()
    {
        ["vaccineName"] = new OpenApiString("Rabies (booster)"),
        ["administeredAt"] = new OpenApiString("2026-04-01T09:00:00Z"),
        ["nextDueAt"] = new OpenApiString("2027-04-01T09:00:00Z"),
        ["batchNumber"] = new OpenApiString("RAB-2026-0412B"),
    };

    private static OpenApiObject ExampleVaccination() => new()
    {
        ["id"] = new OpenApiString("e34a56b7-8901-4c23-9def-456789012345"),
        ["tenantId"] = new OpenApiString("a4f8e6c5-9d2a-4f86-9c11-2b3a82a1d9e2"),
        ["petId"] = new OpenApiString("c97e2d8a-cf9c-4b31-9e15-7a2eb1f8a3c4"),
        ["vaccineName"] = new OpenApiString("Rabies"),
        ["administeredAt"] = new OpenApiString("2026-04-01T09:00:00Z"),
        ["nextDueAt"] = new OpenApiString("2027-04-01T09:00:00Z"),
        ["batchNumber"] = new OpenApiString("RAB-2026-0412"),
        ["createdAt"] = new OpenApiString("2026-05-07T12:00:00Z"),
        ["updatedAt"] = new OpenApiString("2026-05-07T12:00:00Z"),
    };
}
