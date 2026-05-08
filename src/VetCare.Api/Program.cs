using System.Reflection;
using Asp.Versioning;
using Microsoft.OpenApi.Models;
using VetCare.Api.Authorization;
using VetCare.Api.Endpoints.Appointments;
using VetCare.Api.Endpoints.Auth;
using VetCare.Api.Endpoints.Owners;
using VetCare.Api.Endpoints.Pets;
using VetCare.Api.Endpoints.Vaccinations;
using VetCare.Api.Infrastructure;
using VetCare.Application;
using VetCare.Infrastructure;
using VetCare.Domain.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
        policy.RequireRole(nameof(UserRole.Admin)))
    .AddPolicy(AuthorizationPolicies.VetOrAdmin, policy =>
        policy.RequireRole(nameof(UserRole.Vet), nameof(UserRole.Admin)))
    .AddPolicy(AuthorizationPolicies.AnyStaff, policy =>
        policy.RequireRole(nameof(UserRole.Admin), nameof(UserRole.Vet), nameof(UserRole.Receptionist)));

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "VetCare API",
        Version = "v1",
        Description = "VetCare multi-tenant veterinary API.",
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT token issued by /auth/login or /auth/register.",
    });

    options.OperationFilter<SwaggerJwtSecurityFilter>();
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "VetCare API v1");
    });
}

app.UseAuthentication();
app.UseAuthorization();

var versionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .ReportApiVersions()
    .Build();

app.MapGet("/health", () => "ok");

app.MapAuthEndpoints(versionSet);
app.MapOwnerEndpoints(versionSet);
app.MapPetEndpoints(versionSet);
app.MapAppointmentEndpoints(versionSet);
app.MapVaccinationEndpoints(versionSet);

app.Run();

public partial class Program;
