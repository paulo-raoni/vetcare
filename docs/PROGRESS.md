# VetCare — Progress

## M4 — Audit log (MongoDB) + S3 photo upload (in delivery)

- New `ICommand` marker (`Application.Abstractions.Messaging`) applied to all state-mutating MediatR requests (Owner / Pet / Appointment / Vaccination commands + new `UploadPetPhotoCommand`); queries deliberately do not implement it. Auth `Login` / `Register` are excluded so credentials never reach the audit payload.
- `AuditEntry` record (`Id`, `TenantId?`, `UserId?`, `Action`, `EntityType?`, `EntityId?`, `Payload`, `OccurredAt`) and `IAuditRepository` abstraction live in Application; `MongoAuditRepository` (Infrastructure) inserts into MongoDB via `IMongoCollection<BsonDocument>`, payload serialized through `System.Text.Json` then `BsonDocument.Parse`.
- `AuditBehavior<TRequest,TResponse>` runs after the handler succeeds, gates on `request is ICommand`, captures `TenantId` / `UserId` from `ICurrentUserService`, sets `Action = typeof(TRequest).Name`, attaches the request as `Payload`, swallows audit-store failures with a warning. Registered AFTER `ValidationBehavior` in `Application/DependencyInjection.cs`.
- New `MongoDbOptions` (`ConnectionString`, `DatabaseName`, `AuditCollectionName`) bound to `Mongo` config section; `IMongoClient` / `IMongoDatabase` / `IAuditRepository` registered in `Infrastructure/DependencyInjection.cs`. `appsettings.Development.json` adds the `Mongo` section (`mongodb://vetcare:vetcare@localhost:27017`, db `vetcare`, collection `audit_log`).
- `IStorageService` abstraction in Application; `S3StorageService` (Infrastructure) wraps `IAmazonS3.PutObjectAsync`, returns a path-style public URL (`{ServiceUrl}/{Bucket}/{key}` for LocalStack, `https://{Bucket}.s3.amazonaws.com/{key}` otherwise). `S3Options` (`BucketName`, `ServiceUrl`) bound to the `S3` section; `IAmazonS3` client uses `AwsOptions` for credentials/region with `ForcePathStyle` when a `ServiceUrl` is configured.
- `UploadPetPhotoCommand(PetId, FileName, Content (Stream — `[JsonIgnore]`), ContentType)` + handler loads the pet via `PetByIdSpec`, generates a key `pets/{tenantId}/{petId}/{guid}{ext}`, uploads via `IStorageService`, calls `Pet.UpdatePhoto(url)` and persists.
- New endpoint `POST /api/v1/pets/{id}/photo` accepts `multipart/form-data` (`IFormFile file`), `RequireAuthorization()`, `DisableAntiforgery()`, validates `Length ≤ 5 MB` and content type `image/jpeg` / `image/png`, returns `Ok<PetDto>` or `ValidationProblem`; documented via Swagger.
- New packages pinned in `Directory.Packages.props`: `MongoDB.Driver` 2.30.0, `AWSSDK.S3` 3.7.400.5; both referenced by `VetCare.Infrastructure.csproj`.

### Tests added

- Application unit tests: `AuditBehaviorTests` (audits a command after success, skips queries, does not audit when handler throws) and `UploadPetPhotoHandlerTests` (calls `IStorageService.UploadAsync` with the tenant/pet-scoped key, mutates `Pet.PhotoUrl`, throws `NotFoundException` when the pet is missing). Application unit count is 18 → handler/behavior coverage now drives 30 total Application tests.
- Integration tests: `PetPhotoEndpointTests` posts a multipart `image/jpeg` and asserts `200` + returned `PhotoUrl`, `400` on a >5 MB body, `400` on `application/pdf`. `VetCareWebApplicationFactory` now substitutes `IAmazonS3`, `IStorageService`, `IMongoClient`, `IMongoDatabase`, and `IAuditRepository`, and asserts that `IAuditRepository.SaveAsync` is invoked for the upload command (`Action == "UploadPetPhotoCommand"`).

### Gates

- `make build` — 0 warnings, 0 errors.
- `make test` — 78 passing (Domain 27, Application 30, Integration 21).
- `make lint` — clean.

## M3 — Appointments + Vaccinations (in delivery)

- `Appointment` aggregate with `Scheduled → Confirmed → Completed` / `Cancelled` state machine, `DomainException`-backed transition guards, `Notes` ≤ 1000 chars, future-only `ScheduledAt`; raises `AppointmentScheduledEvent`, `AppointmentCancelledEvent`, `AppointmentCompletedEvent`.
- `Vaccination` aggregate with non-empty `VaccineName`, non-future `AdministeredAt`, optional `NextDueAt ≥ AdministeredAt`, mandatory `BatchNumber`; raises `VaccinationRecordedEvent`. Both aggregates are `ITenantEntity` and ignored in EF for `DomainEvents`.
- Application layer adds `ScheduleAppointment` / `ConfirmAppointment` / `CancelAppointment` / `CompleteAppointment` commands, `RecordVaccination` / `UpdateVaccination` commands, paginated `GetById` / `List` queries (filterable by `PetId`, `Status`, date range), Mapster registers, FluentValidation validators, and `AppointmentByIdSpec` / `AppointmentListSpec` / `VaccinationByIdSpec` / `VaccinationListSpec`.
- New `ISqsPublisher` abstraction (`Application.Abstractions.Messaging`) with `QueueNames` constants. MediatR `INotificationHandler`s `OnAppointmentScheduled` / `OnAppointmentCancelled` publish JSON `AppointmentReminderMessage` payloads to `appointment-reminders` and `appointment-cancellations`.
- `VetCareDbContext.SaveChangesAsync` collects pending `DomainEvents` from tracked aggregates and dispatches them via MediatR `IPublisher` after the transaction commits, wiring the M2 events to runtime handlers for the first time.
- Infrastructure adds `SqsPublisher` over `IAmazonSQS` (cached `GetQueueUrl`), `AwsOptions` for `Region` / `ServiceUrl` / credentials (LocalStack-friendly), and `AppointmentRepository` / `VaccinationRepository` registered as `IRepository<Appointment>` / `IRepository<Vaccination>`.
- New EF migration `AddAppointmentsAndVaccinations` creates `vetcare.appointments` and `vetcare.vaccinations` with `(TenantId, PetId)` and `(TenantId, ScheduledAt)` indexes; tenant query filters extended to both new tables.
- Minimal-API endpoints under `/api/v1/appointments` (`POST`, `GET`, `GET {id}`, `PUT {id}/confirm`, `PUT {id}/cancel`, `PUT {id}/complete`) and `/api/v1/vaccinations` (`POST`, `GET`, `GET {id}`, `PUT {id}`), all `RequireAuthorization()` with Swagger examples; `GlobalExceptionHandler` now maps `DomainException` → `409 Conflict`.
- E2E HTTP test suite at `docs/e2e/vetcare.collection.json` (Postman v2.1 collection) plus `docs/e2e/README.md` documenting `newman run docs/e2e/vetcare.collection.json --env-var baseUrl=http://localhost:5000` for the full register → login → owner → pet → appointment → confirm → vaccination flow.

### Tests added

- Domain unit tests: `AppointmentTests` (status transitions, invariants, event raising), `VaccinationTests` (invariants, event raising) — 13 new tests.
- Application unit tests: `ScheduleAppointmentHandlerTests`, `CancelAppointmentHandlerTests`, `OnAppointmentScheduledTests`, `RecordVaccinationHandlerTests` — handler success / `NotFoundException` / `DomainException` paths plus validator coverage; `OnAppointmentScheduled` verified to invoke `ISqsPublisher` once with `QueueNames.AppointmentReminders`.
- Integration tests: `AppointmentEndpointsTests` runs the full happy path (register → login → owner → pet → schedule → confirm → complete), filters list by `petId`, asserts `409` on illegal transitions, exercises `POST /api/v1/vaccinations` (201 + 400 on future date), and asserts `ISqsPublisher.PublishAsync` is invoked. `WebApplicationFactory` substitutes `IAmazonSQS` and `ISqsPublisher` with NSubstitute fakes.

### Gates

- `make build` — 0 warnings, 0 errors.
- `make test` — 70 passing (Domain 27, Application 25, Integration 18).
- `make lint` — clean.

## M2 — Pets + Owners CRUD (in delivery)

- Specification pattern in `VetCare.Application.Abstractions.Specifications` (`ISpecification<T>`, `Specification<T>` base, `SpecificationEvaluator<T>`); concrete specs `OwnerByIdSpec`, `OwnerListSpec`, `PetByIdSpec`, `PetListSpec`, `PetsByOwnerSpec`.
- Generic `IRepository<T>` abstraction in Application; `EfRepository<T>` base in Infrastructure with `OwnerRepository` and `PetRepository` implementations registered as `IRepository<Owner>` / `IRepository<Pet>`.
- Owner CRUD: `CreateOwner`, `UpdateOwner`, `DeleteOwner` commands + `GetOwnerById`, `ListOwners` queries — each with MediatR handler, FluentValidation validator, and Mapster mapping (`OwnerMappingConfig : IRegister`).
- Pet CRUD: `CreatePet`, `UpdatePet`, `DeletePet` commands + `GetPetById`, `ListPets` (filterable by `OwnerId`) queries — each with handler, validator, and Mapster mapping (`PetMappingConfig`).
- `ValidationBehavior<TRequest,TResponse>` MediatR pipeline runs all registered FluentValidation validators and short-circuits with `ValidationException` before handlers execute.
- Domain extensions: `Owner.UpdateContact(...)` and `Pet.UpdateProfile(...)` to support update flows under aggregate invariants.
- Minimal-API endpoints under `/api/v1/owners` and `/api/v1/pets`, all `RequireAuthorization()`, returning `Ok<T>` / `Created<T>` / `NoContent` typed results, with `ProducesValidationProblem` and `ProducesProblem(404)` documented.
- Swagger: `Swashbuckle.AspNetCore` registered with JWT bearer security definition, XML comments, and inline OpenAPI request/response examples for create/get owner and create/get pet.
- `GlobalExceptionHandler` (`IExceptionHandler`) translates `FluentValidation.ValidationException` to RFC 7231 `400 ValidationProblemDetails` and `NotFoundException` to `404 ProblemDetails`; tenant scoping continues to be enforced by EF Core query filters bound to `ITenantProvider`.
- JWT bearer options now configured via `IOptions<JwtOptions>` so test/host configuration overrides are honored without re-registering authentication.

### Tests added

- Application unit tests: `CreateOwnerHandlerTests`, `CreatePetHandlerTests`, and `ValidationBehaviorTests` (covering happy path, validation short-circuit, and missing-tenant / missing-owner failure modes).
- Integration tests: `OwnerEndpointsTests` and `PetEndpointsTests` exercise register → JWT → CRUD across `/api/v1/owners` and `/api/v1/pets`, plus 401 without JWT, 400 on invalid email, and 404 on unknown owner.

### Gates

- `make build` — 0 warnings, 0 errors.
- `make test` — 39 passing (Domain 14, Application 13, Integration 12).
- `make lint` — clean.
