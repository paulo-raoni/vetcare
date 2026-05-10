using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using VetCare.Application.Common.Exceptions;
using VetCare.Domain.Primitives;

namespace VetCare.Api.Infrastructure;

internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        switch (exception)
        {
            case ValidationException validation:
                {
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    var errors = validation.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                    var problem = new ValidationProblemDetails(errors)
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                        Title = "One or more validation errors occurred.",
                    };

                    return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
                    {
                        HttpContext = httpContext,
                        ProblemDetails = problem,
                        Exception = exception,
                    });
                }

            case NotFoundException notFound:
                {
                    httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                    var problem = new ProblemDetails
                    {
                        Status = StatusCodes.Status404NotFound,
                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                        Title = "Resource not found.",
                        Detail = notFound.Message,
                    };

                    return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
                    {
                        HttpContext = httpContext,
                        ProblemDetails = problem,
                        Exception = exception,
                    });
                }

            case DomainException domain:
                {
                    httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
                    var problem = new ProblemDetails
                    {
                        Status = StatusCodes.Status409Conflict,
                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                        Title = "Domain rule violation.",
                        Detail = domain.Message,
                    };

                    return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
                    {
                        HttpContext = httpContext,
                        ProblemDetails = problem,
                        Exception = exception,
                    });
                }

            case DbUpdateException { InnerException: PostgresException { SqlState: "23503" } }:
                {
                    httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
                    var problem = new ProblemDetails
                    {
                        Status = StatusCodes.Status409Conflict,
                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                        Title = "Cannot delete resource because it is referenced by other entities.",
                        Detail = "The resource has dependent records.",
                    };

                    return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
                    {
                        HttpContext = httpContext,
                        ProblemDetails = problem,
                        Exception = exception,
                    });
                }

            default:
                return false;
        }
    }
}
