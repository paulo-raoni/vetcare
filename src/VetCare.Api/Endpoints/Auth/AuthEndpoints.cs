using Asp.Versioning;
using Asp.Versioning.Builder;
using MediatR;
using VetCare.Application.Auth;
using VetCare.Application.Auth.Login;
using VetCare.Application.Auth.Register;

namespace VetCare.Api.Endpoints.Auth;

internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app, ApiVersionSet versionSet)
    {
        var group = app
            .MapGroup("/api/v{version:apiVersion}/auth")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(new ApiVersion(1))
            .WithTags("Auth");

        group.MapPost("/register", Register)
            .AllowAnonymous()
            .WithName("Register");

        group.MapPost("/login", Login)
            .AllowAnonymous()
            .WithName("Login");

        return app;
    }

    internal sealed record RegisterRequest(string TenantName, string TenantSlug, string Email, string Password);

    internal sealed record LoginRequest(string TenantSlug, string Email, string Password);

    internal static async Task<IResult> Register(RegisterRequest request, ISender sender, CancellationToken ct)
    {
        try
        {
            var result = await sender.Send(
                new RegisterCommand(request.TenantName, request.TenantSlug, request.Email, request.Password),
                ct);
            return Results.Ok(result);
        }
        catch (TenantSlugAlreadyExistsException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    internal static async Task<IResult> Login(LoginRequest request, ISender sender, CancellationToken ct)
    {
        try
        {
            var result = await sender.Send(
                new LoginCommand(request.TenantSlug, request.Email, request.Password),
                ct);
            return Results.Ok(result);
        }
        catch (InvalidCredentialsException)
        {
            return Results.Unauthorized();
        }
    }
}
