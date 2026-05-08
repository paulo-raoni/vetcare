using Asp.Versioning;
using Asp.Versioning.Builder;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using VetCare.Api.Authorization;
using VetCare.Application.Appointments;
using VetCare.Application.Appointments.Commands.CancelAppointment;
using VetCare.Application.Appointments.Commands.CompleteAppointment;
using VetCare.Application.Appointments.Commands.ConfirmAppointment;
using VetCare.Application.Appointments.Commands.ScheduleAppointment;
using VetCare.Application.Appointments.Queries.GetAppointmentById;
using VetCare.Application.Appointments.Queries.ListAppointments;
using VetCare.Application.Common.Pagination;
using VetCare.Domain.Appointments;

namespace VetCare.Api.Endpoints.Appointments;

internal static class AppointmentEndpoints
{
    public static IEndpointRouteBuilder MapAppointmentEndpoints(this IEndpointRouteBuilder app, ApiVersionSet versionSet)
    {
        var group = app
            .MapGroup("/api/v{version:apiVersion}/appointments")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(new ApiVersion(1))
            .RequireAuthorization()
            .WithTags("Appointments");

        group.MapGet("/", ListAppointments)
            .WithName("ListAppointments")
            .RequireAuthorization(AuthorizationPolicies.AnyStaff)
            .Produces<PagedResult<AppointmentDto>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .WithOpenApi(op =>
            {
                op.Summary = "List appointments (paginated, filterable by petId, status, date range)";
                op.Description = "Returns a paginated list of appointments scoped to the caller's tenant. Optionally filter by petId, status, and ScheduledAt range.";
                return op;
            });

        group.MapGet("/{id:guid}", GetAppointmentById)
            .WithName("GetAppointmentById")
            .RequireAuthorization(AuthorizationPolicies.AnyStaff)
            .Produces<AppointmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi(op =>
            {
                op.Summary = "Get an appointment by id";
                op.Responses["200"].Content["application/json"].Example = ExampleAppointment();
                return op;
            });

        group.MapPost("/", ScheduleAppointment)
            .WithName("ScheduleAppointment")
            .RequireAuthorization(AuthorizationPolicies.AnyStaff)
            .Produces<AppointmentDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi(op =>
            {
                op.Summary = "Schedule a new appointment";
                op.RequestBody.Content["application/json"].Example = ExampleScheduleAppointment();
                op.Responses["201"].Content["application/json"].Example = ExampleAppointment();
                return op;
            });

        group.MapPut("/{id:guid}/confirm", ConfirmAppointment)
            .WithName("ConfirmAppointment")
            .RequireAuthorization(AuthorizationPolicies.VetOrAdmin)
            .Produces<AppointmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithOpenApi(op =>
            {
                op.Summary = "Confirm a scheduled appointment";
                return op;
            });

        group.MapPut("/{id:guid}/cancel", CancelAppointment)
            .WithName("CancelAppointment")
            .RequireAuthorization(AuthorizationPolicies.AnyStaff)
            .Produces<AppointmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithOpenApi(op =>
            {
                op.Summary = "Cancel an appointment";
                return op;
            });

        group.MapPut("/{id:guid}/complete", CompleteAppointment)
            .WithName("CompleteAppointment")
            .RequireAuthorization(AuthorizationPolicies.VetOrAdmin)
            .Produces<AppointmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithOpenApi(op =>
            {
                op.Summary = "Mark a confirmed appointment as completed";
                return op;
            });

        return app;
    }

    internal sealed record ScheduleAppointmentRequest(
        Guid PetId,
        Guid VetUserId,
        DateTime ScheduledAt,
        string? Notes);

    private static async Task<Ok<PagedResult<AppointmentDto>>> ListAppointments(
        ISender sender,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? petId = null,
        [FromQuery] AppointmentStatus? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var result = await sender.Send(new ListAppointmentsQuery(page, pageSize, petId, status, from, to), cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<AppointmentDto>> GetAppointmentById(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAppointmentByIdQuery(id), cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Created<AppointmentDto>> ScheduleAppointment(
        ScheduleAppointmentRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ScheduleAppointmentCommand(request.PetId, request.VetUserId, request.ScheduledAt, request.Notes),
            cancellationToken);
        return TypedResults.Created($"/api/v1/appointments/{result.Id}", result);
    }

    private static async Task<Ok<AppointmentDto>> ConfirmAppointment(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ConfirmAppointmentCommand(id), cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<AppointmentDto>> CancelAppointment(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CancelAppointmentCommand(id), cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<AppointmentDto>> CompleteAppointment(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CompleteAppointmentCommand(id), cancellationToken);
        return TypedResults.Ok(result);
    }

    private static OpenApiObject ExampleScheduleAppointment() => new()
    {
        ["petId"] = new OpenApiString("c97e2d8a-cf9c-4b31-9e15-7a2eb1f8a3c4"),
        ["vetUserId"] = new OpenApiString("8f4e2d5c-71b3-4d9a-9876-1a2b3c4d5e6f"),
        ["scheduledAt"] = new OpenApiString("2026-06-15T14:30:00Z"),
        ["notes"] = new OpenApiString("Annual check-up"),
    };

    private static OpenApiObject ExampleAppointment() => new()
    {
        ["id"] = new OpenApiString("d12b34c5-67a8-4f9e-bc01-234567890abc"),
        ["tenantId"] = new OpenApiString("a4f8e6c5-9d2a-4f86-9c11-2b3a82a1d9e2"),
        ["petId"] = new OpenApiString("c97e2d8a-cf9c-4b31-9e15-7a2eb1f8a3c4"),
        ["vetUserId"] = new OpenApiString("8f4e2d5c-71b3-4d9a-9876-1a2b3c4d5e6f"),
        ["scheduledAt"] = new OpenApiString("2026-06-15T14:30:00Z"),
        ["status"] = new OpenApiInteger(1),
        ["notes"] = new OpenApiString("Annual check-up"),
        ["createdAt"] = new OpenApiString("2026-05-07T12:00:00Z"),
        ["updatedAt"] = new OpenApiString("2026-05-07T12:00:00Z"),
    };
}
