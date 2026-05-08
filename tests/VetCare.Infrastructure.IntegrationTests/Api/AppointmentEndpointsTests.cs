using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using NSubstitute;
using VetCare.Application.Abstractions.Messaging;
using VetCare.Application.Appointments.Events;
using VetCare.Domain.Appointments;
using VetCare.Domain.Pets;
using VetCare.Domain.Users;

namespace VetCare.Infrastructure.IntegrationTests.Api;

public sealed class AppointmentEndpointsTests : IClassFixture<VetCareWebApplicationFactory>
{
    private readonly VetCareWebApplicationFactory _factory;

    public AppointmentEndpointsTests(VetCareWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private sealed record AuthResponse(string AccessToken, DateTime ExpiresAt, Guid TenantId, Guid UserId, string Email, string Role);

    private sealed record OwnerResponse(Guid Id, Guid TenantId, string FullName, string Phone, string Email, DateTime CreatedAt, DateTime UpdatedAt);

    private sealed record PetResponse(
        Guid Id,
        Guid TenantId,
        Guid OwnerId,
        string Name,
        Species Species,
        string Breed,
        DateOnly DateOfBirth,
        string? PhotoUrl,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    private sealed record AppointmentResponse(
        Guid Id,
        Guid TenantId,
        Guid PetId,
        Guid VetUserId,
        DateTime ScheduledAt,
        AppointmentStatus Status,
        string? Notes,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    private sealed record VaccinationResponse(
        Guid Id,
        Guid TenantId,
        Guid PetId,
        string VaccineName,
        DateTime AdministeredAt,
        DateTime? NextDueAt,
        string BatchNumber,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    private sealed record PagedAppointments(IReadOnlyList<AppointmentResponse> Items, int Page, int PageSize, int TotalCount, int TotalPages);

    private async Task<(HttpClient Client, AuthResponse Auth)> AuthenticatedClient()
    {
        var client = _factory.CreateClient();
        var slug = $"clinic-{Guid.NewGuid():N}";

        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantName = "Acme Vet",
            tenantSlug = slug,
            email = $"admin-{Guid.NewGuid():N}@acme.test",
            password = "ChangeMe123!",
        });
        register.StatusCode.Should().Be(HttpStatusCode.OK);

        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return (client, auth);
    }

    private static async Task<OwnerResponse> CreateOwner(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/owners", new
        {
            fullName = "Jane Doe",
            phone = "+5511999999999",
            email = $"jane-{Guid.NewGuid():N}@example.com",
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<OwnerResponse>())!;
    }

    private static async Task<PetResponse> CreatePet(HttpClient client, Guid ownerId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/pets", new
        {
            ownerId,
            name = "Rex",
            species = (int)Species.Dog,
            breed = "Labrador",
            dateOfBirth = "2022-04-15",
            photoUrl = (string?)null,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<PetResponse>())!;
    }

    [Fact]
    public async Task Full_flow_register_login_owner_pet_appointment_confirm_complete()
    {
        var (client, auth) = await AuthenticatedClient();
        var owner = await CreateOwner(client);
        var pet = await CreatePet(client, owner.Id);
        var vetUserId = await _factory.CreateUserAsync(auth.TenantId, UserRole.Vet);

        var scheduledAt = DateTime.UtcNow.AddDays(2);
        var scheduleResponse = await client.PostAsJsonAsync("/api/v1/appointments", new
        {
            petId = pet.Id,
            vetUserId,
            scheduledAt,
            notes = "Annual check-up",
        });

        scheduleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var appointment = await scheduleResponse.Content.ReadFromJsonAsync<AppointmentResponse>();
        appointment.Should().NotBeNull();
        appointment!.PetId.Should().Be(pet.Id);
        appointment.Status.Should().Be(AppointmentStatus.Scheduled);

        await _factory.SqsPublisher.Received().PublishAsync(
            QueueNames.AppointmentReminders,
            Arg.Any<AppointmentReminderMessage>(),
            Arg.Any<CancellationToken>());

        var confirmResponse = await client.PutAsync($"/api/v1/appointments/{appointment.Id}/confirm", null);
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var confirmed = await confirmResponse.Content.ReadFromJsonAsync<AppointmentResponse>();
        confirmed!.Status.Should().Be(AppointmentStatus.Confirmed);

        var completeResponse = await client.PutAsync($"/api/v1/appointments/{appointment.Id}/complete", null);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var completed = await completeResponse.Content.ReadFromJsonAsync<AppointmentResponse>();
        completed!.Status.Should().Be(AppointmentStatus.Completed);
    }

    [Fact]
    public async Task Schedule_appointment_without_jwt_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/appointments", new
        {
            petId = Guid.NewGuid(),
            vetUserId = Guid.NewGuid(),
            scheduledAt = DateTime.UtcNow.AddDays(1),
            notes = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_appointments_filtered_by_pet_id()
    {
        var (client, auth) = await AuthenticatedClient();
        var owner = await CreateOwner(client);
        var pet1 = await CreatePet(client, owner.Id);
        var pet2 = await CreatePet(client, owner.Id);
        var vetUserId = await _factory.CreateUserAsync(auth.TenantId, UserRole.Vet);

        var schedule1 = await client.PostAsJsonAsync("/api/v1/appointments", new
        {
            petId = pet1.Id,
            vetUserId,
            scheduledAt = DateTime.UtcNow.AddDays(1),
            notes = (string?)null,
        });
        schedule1.StatusCode.Should().Be(HttpStatusCode.Created);

        var schedule2 = await client.PostAsJsonAsync("/api/v1/appointments", new
        {
            petId = pet2.Id,
            vetUserId,
            scheduledAt = DateTime.UtcNow.AddDays(2),
            notes = (string?)null,
        });
        schedule2.StatusCode.Should().Be(HttpStatusCode.Created);

        var listResponse = await client.GetAsync($"/api/v1/appointments?petId={pet1.Id}");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await listResponse.Content.ReadFromJsonAsync<PagedAppointments>();
        page.Should().NotBeNull();
        page!.Items.Should().OnlyContain(a => a.PetId == pet1.Id);
        page.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Confirm_then_cancel_returns_409_after_cancel()
    {
        var (client, auth) = await AuthenticatedClient();
        var owner = await CreateOwner(client);
        var pet = await CreatePet(client, owner.Id);
        var vetUserId = await _factory.CreateUserAsync(auth.TenantId, UserRole.Vet);

        var scheduleResponse = await client.PostAsJsonAsync("/api/v1/appointments", new
        {
            petId = pet.Id,
            vetUserId,
            scheduledAt = DateTime.UtcNow.AddDays(1),
            notes = (string?)null,
        });
        var appointment = (await scheduleResponse.Content.ReadFromJsonAsync<AppointmentResponse>())!;

        var cancel = await client.PutAsync($"/api/v1/appointments/{appointment.Id}/cancel", null);
        cancel.StatusCode.Should().Be(HttpStatusCode.OK);

        var confirmAgain = await client.PutAsync($"/api/v1/appointments/{appointment.Id}/confirm", null);
        confirmAgain.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Record_vaccination_returns_201_and_can_be_listed()
    {
        var (client, _) = await AuthenticatedClient();
        var owner = await CreateOwner(client);
        var pet = await CreatePet(client, owner.Id);

        var administered = DateTime.UtcNow.AddDays(-3);
        var recordResponse = await client.PostAsJsonAsync("/api/v1/vaccinations", new
        {
            petId = pet.Id,
            vaccineName = "Rabies",
            administeredAt = administered,
            nextDueAt = administered.AddYears(1),
            batchNumber = "RAB-2026-0412",
        });

        recordResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var vaccination = await recordResponse.Content.ReadFromJsonAsync<VaccinationResponse>();
        vaccination.Should().NotBeNull();
        vaccination!.PetId.Should().Be(pet.Id);
        vaccination.VaccineName.Should().Be("Rabies");

        var getResponse = await client.GetAsync($"/api/v1/vaccinations/{vaccination.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Record_vaccination_with_future_date_returns_400()
    {
        var (client, _) = await AuthenticatedClient();
        var owner = await CreateOwner(client);
        var pet = await CreatePet(client, owner.Id);

        var response = await client.PostAsJsonAsync("/api/v1/vaccinations", new
        {
            petId = pet.Id,
            vaccineName = "Rabies",
            administeredAt = DateTime.UtcNow.AddDays(1),
            nextDueAt = (DateTime?)null,
            batchNumber = "RAB-001",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
