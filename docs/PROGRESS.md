# VetCare — Progress

## M8 — Transactional Outbox (delivered)

- **Closes the ADR-006 reliability gap.** ADR-006 records that the SQS publish in `VetCareDbContext.SaveChangesAsync` runs post-commit but not transactionally; M8 promotes the outbox follow-up out of `BACKLOG.md` into a concrete milestone so a crash between commit and publish no longer drops the SQS message.
- **Four-PR implementation order.** (1) `feat/m8-outbox-domain-event-metadata` adds stable `EventId` / `OccurredOnUtc` to `IDomainEvent` and the six event types; (2) `feat/m8-outbox-table-and-write-path` adds `OutboxMessage` + the migration + the `SaveChangesAsync` write, while keeping the existing post-commit publish so production behavior is unchanged; (3) `feat/m8-outbox-worker` adds `OutboxProcessor` + `OutboxOptions`; (4) `feat/m8-outbox-cutover` removes the post-commit publish so the worker becomes the only delivery path.
- **Domain-layer breaking change to `IDomainEvent`.** The current default-implemented `EventId` / `OccurredOn` getters return fresh values on every read, which is unsuitable for outbox round-trips. M8 makes both members constructor-set and renames `OccurredOn` to `OccurredOnUtc`. The six existing events (`AppointmentScheduledEvent`, `AppointmentCancelledEvent`, `AppointmentCompletedEvent`, `VaccinationRecordedEvent`, `OwnerCreatedEvent`, `PetRegisteredEvent`) absorb the change in PR (1).
- **New EF migration target `AddOutboxMessages`.** Creates `vetcare.OutboxMessage` (`Id`, `OccurredOnUtc`, `Type`, `Content (jsonb)`, `TenantId`, `ProcessedOnUtc`, `Error`, `Attempts`) with a `(ProcessedOnUtc, OccurredOnUtc)` index for the worker's next-batch scan. Backout is purely additive — drop migration, remove worker, restore the synchronous publish.
- **New `OutboxProcessor : BackgroundService`.** Polls every 2 s (configurable), reads up to 50 rows per batch with `SELECT ... FOR UPDATE SKIP LOCKED` so multi-instance deploys do not double-publish, hydrates events via `Type.GetType`, calls the in-process `IPublisher`, marks `ProcessedOnUtc` on success and increments `Attempts` + stores `Error` on failure with exponential backoff and a 30 s ceiling. After `MaxAttempts` (default 10) the row is parked as poison and surfaced via a warning log.
- **Tenant propagation for non-HTTP scope.** `OutboxMessage` carries `TenantId`; the worker resolves a per-message `IServiceScope` and sets `ITenantProvider` to the row's tenant before resolving `IPublisher`. Both existing handlers (`OnAppointmentScheduled`, `OnAppointmentCancelled`) read `TenantId` from the event payload and depend only on `ISqsPublisher`, so they replay safely under the worker scope.
- **Test strategy.** Domain unit tests cover the `IDomainEvent` constructor + per-event JSON round-trip; Application tests cover the `SaveChangesAsync` write path (happy + no-events); Testcontainers integration tests cover failing-publisher → outbox row + `Error` + `Attempts == 1`, recovery → `ProcessedOnUtc` set, and exceeding `MaxAttempts` → poison + warning logged. Newman E2E exercises the worker indirectly through the existing schedule/confirm/complete flow.
- **Risks.** `Type.GetType(assemblyQualifiedName)` is fragile to renames or assembly moves (mitigation: optional registry, future-proofing only); poison rows accumulate without a dead-letter table (out of scope for M8); the worker shares the request-path Postgres — small batch + indexed scan + 2 s poll keeps pressure low, revisit if integration timing surfaces contention.
- **Delivery summary.** Four PRs landed back-to-back:
  `feat/m8-outbox-domain-event-metadata` (PR 1, +44/-9 across 9 files;
  Domain tests 27 → 39), `feat/m8-outbox-table-and-write-path`
  (PR 2, 7 files; Integration tests 42 → 45),
  `feat/m8-outbox-worker` (PR 3, 13 files; Integration tests 45 → 50,
  includes a follow-up migration `AddOutboxMessageNextAttemptOnUtc`
  for persistent backoff), and `feat/m8-outbox-cutover` (PR 4, 2
  files, removes the post-commit publish so the worker becomes the
  sole delivery path). Final gates clean: 0 warnings, 0 errors,
  Domain 39 + Application 36 + Integration 50, lint pass. ADR-009
  flipped to Accepted; outbox entry in `BACKLOG.md` flipped to
  delivered with a dead-letter follow-up promoted in its place.

## feat/testcontainers — Real Postgres + Mongo in integration tests (in delivery)

- **Replaced EF InMemory with a real PostgreSQL container.** `VetCareWebApplicationFactory` now implements `IAsyncLifetime`, spins up `postgres:16` via `Testcontainers.PostgreSql`, and runs `Database.MigrateAsync()` on startup so the integration suite exercises the actual Npgsql provider, the `vetcare` schema migration, query filters, FK constraints, and composite indexes — bugs that EF InMemory's permissive translator silently masked.
- **Replaced the NSubstitute `IAuditRepository` with the real `MongoAuditRepository`.** A `mongo:7` container backs every test class; `IMongoClient` / `IMongoDatabase` resolve through `IOptions<MongoDbOptions>` so the production wiring is exercised end-to-end. Audit assertions now query Mongo directly via a new `CountAuditEntriesByActionAsync` helper.
- **Single shared container fixture per collection.** Added `IntegrationTestsCollection` (`[CollectionDefinition]` + `ICollectionFixture<VetCareWebApplicationFactory>`) and converted every API test class to `[Collection(IntegrationTestsCollection.Name)]`, so the suite spins up one Postgres + one Mongo for the whole run instead of one pair per test class.
- **DbContext re-registered inside `ConfigureServices`.** `AddInfrastructure` captures the connection string at registration time, so the InMemory configuration override alone is not enough — the factory now removes the `DbContextOptions<VetCareDbContext>` descriptor and re-adds `UseNpgsql(_postgres.GetConnectionString(), …)` with the correct migrations history table. Mongo and AWS clients use lazy `IOptions` resolution, so the InMemory config override is sufficient there.
- **`ISqsPublisher` and `IStorageService` stay as NSubstitute fakes.** Bringing LocalStack into Testcontainers would require additional bootstrap (queue creation, S3 bucket creation, region/credentials wiring) that does not pay off for the current assertions; tracked in `docs/BACKLOG.md`. SQS / S3 round-trips remain covered by the Newman E2E job below.
- **Newman E2E CI job added.** New `e2e` job in `.github/workflows/ci.yml` runs after `build-and-test` on `ubuntu-latest`: `docker compose up postgres mongo localstack` → `dotnet ef database update` → `dotnet run --project src/VetCare.Api --urls http://localhost:5000` (background) → poll `/health` until 200 → `npm install -g newman` → `newman run docs/e2e/vetcare.collection.json --env-var baseUrl=http://localhost:5000` → `docker compose down -v` (always).
- **Tests adjusted for real Postgres.** Existing tests already used unique tenant slugs (`clinic-{Guid.NewGuid():N}`) and unique emails on user creation, which carry over to the shared database without collisions; no behavioral test changes were needed beyond the audit assertion in `PetPhotoEndpointTests` switching from NSubstitute `.Received()` to a Mongo `CountDocuments` query.
- **Packages.** `Directory.Packages.props` adds `Testcontainers.PostgreSql` 3.10.0 and `Testcontainers.MongoDb` 3.10.0 alongside the existing `Testcontainers` base; `Microsoft.EntityFrameworkCore.InMemory` is no longer referenced from the integration test csproj.
- **Backlog updated.** `docs/BACKLOG.md` marks the Testcontainers item as done for Postgres + Mongo and keeps LocalStack-backed S3/SQS round-trips listed as the remaining piece.
- Gates: `make build` — 0 warnings, 0 errors; `make test` — 88 passing (Domain 27, Application 35, Integration 26) against real Postgres + Mongo; `make lint` — clean.

## M7 — Repository documentation (in delivery)

- **Top-level `README.md` written for an interview-grade first impression.** Twelve sections covering header (CI / .NET 8 / MIT badges), overview with ten technical highlights, ASCII Clean Architecture diagram, project tree, tech-stack table, getting-started (devcontainer + Compose + EF migrations + port-5000 override), tests, Newman e2e, full API reference table, top-five ADR summaries, legacy-module rationale, CI/CD, backlog highlights, and license.
- **Run instructions aligned to the Newman collection.** `dotnet run --project src/VetCare.Api --urls http://localhost:5000` is documented end-to-end so the README, Swagger URL, and `docs/e2e/README.md` all target the same port (default `launchSettings.json` profile is `5271`; `--urls` overrides it without touching the sensitive launch profile).
- **API reference table groups every endpoint by resource** (`Auth`, `Owners`, `Pets`, `Appointments`, `Vaccinations`) with `Method · Path · Auth · Role · Description` columns, sourced directly from `src/VetCare.Api/Endpoints/*` — the role column matches the `AnyStaff` / `VetOrAdmin` policies wired in `Program.cs` after `fix/security-integrity`.
- **Top-five ADRs summarised inline.** ADR-001 (layers), ADR-002 (MediatR + CQRS), ADR-003 (multi-tenant query filters), ADR-004 (Mongo audit), ADR-006 (SQS for appointment events) — each one paragraph, each links back to `docs/DECISIONS.md` for the full ADR.
- **Legacy module section explains the *why*.** Calls out brownfield realism: central package management opt-out for `net48`, Linux/Windows asymmetry handled by the dedicated `legacy-build` GitHub Actions job, `<IsTestProject>false</IsTestProject>` to skip on Linux, and the explicit `tenantId` parameter that compensates for raw SQL bypassing the EF Core query filter.
- **`.env.example` created at the repo root** with placeholder-only values for every env var the API consumes: `ASPNETCORE_ENVIRONMENT` / `ASPNETCORE_URLS`, `ConnectionStrings__DefaultConnection`, `Jwt__{Secret,Issuer,Audience,ExpiryMinutes}`, `Aws__{Region,ServiceUrl,AccessKey,SecretKey}`, `S3__{BucketName,ServiceUrl}`, `Mongo__{ConnectionString,DatabaseName,AuditCollectionName}`. Documents the ASP.NET Core double-underscore convention and notes that the dev defaults in `appsettings.Development.json` already match `docker-compose.yml`, so the file is only required for production-like overrides.
- **Backlog section names the three highest-leverage deferred items** — Testcontainers, outbox pattern for `SaveChangesAsync` → SQS reliability, rate limiting + lockout on auth endpoints — with short descriptions that match `docs/BACKLOG.md`.
- **No code changes.** Doc-only PR; per the project documentation conventions for milestone delivery the `make build` / `make test` / `make lint` gates are not required for `.md`-only deliveries, but were re-run as a sanity check (all clean: 88 tests passing, 0 warnings, 0 errors).

## fix/quality — M0–M5 review fallout (in delivery)

- **Read queries no longer track entities.** `EfRepository.ListAsync` and `EfRepository.CountAsync` apply `Set.AsNoTracking()` before evaluating the spec; `GetByIdAsync` (used by command handlers) stays tracked.
- **New `IRepository<T>.GetByIdAsyncNoTracking` variant.** Implemented in `EfRepository` via `Set.AsNoTracking().FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id)`; tenant global query filters still apply, so multi-tenant isolation is preserved.
- **All four `GetById` query handlers switched to no-tracking.** `GetOwnerByIdQueryHandler`, `GetPetByIdQueryHandler`, `GetAppointmentByIdQueryHandler`, `GetVaccinationByIdQueryHandler` now call `GetByIdAsyncNoTracking(request.Id)` instead of `SingleOrDefaultAsync(new ...ByIdSpec(...))`. The spec classes remain in use by command handlers (CreatePet, DeleteOwner, UpdatePet, ScheduleAppointment, etc.) where tracking is required.
- **List query handlers automatically benefit.** `ListOwners`, `ListPets`, `ListAppointments`, `ListVaccinations` reuse `IRepository.ListAsync`/`CountAsync`, which now run `AsNoTracking` end-to-end.
- **Composite indexes added to four EF configurations.** `AppointmentConfiguration` gains `(TenantId, Status)`; `VaccinationConfiguration` gains `(TenantId, AdministeredAt)`; `OwnerConfiguration` gains `(TenantId, FullName)`; `PetConfiguration` gains `(TenantId, Name)`. Existing `(TenantId, PetId)` / `(TenantId, ScheduledAt)` / `(TenantId, OwnerId)` / `(TenantId, Email)` indexes were already in place from earlier milestones.
- **Migration `20260508193805_AddCompositeIndexes`.** Generated via `dotnet ef migrations add` against `src/VetCare.Infrastructure`; creates the four new indexes in the `vetcare` schema with matching `Down` drops; designer + snapshot updated.
- **Dockerfile hardened.** Final stage now installs `curl`, creates a non-root `appuser` (`adduser --disabled-password --gecos ""`), `chown`s `/app`, copies the published payload with `--chown=appuser:appuser`, switches to `USER appuser` before `ENTRYPOINT`, and adds `HEALTHCHECK --interval=30s --timeout=5s --retries=3 CMD curl -f http://localhost:8080/health || exit 1`.
- **Governance docs created.** `docs/DECISIONS.md` adds eight ADRs covering Clean Architecture, MediatR + CQRS, multi-tenant query filters, MongoDB audit, S3 photo storage, SQS events, JWT + BCrypt auth, and the Specification pattern. `docs/BACKLOG.md` lists the seven deferred improvements from the review (Testcontainers, outbox, rate limiting, AdminOnly endpoints, exhaustive 403 coverage, stable domain-event metadata, removing EF symbols from Application).
- Gates: `make build` — 0/0; `make test` — 88 passing (Domain 27, Application 35, Integration 26); `make lint` — clean.

## fix/security-integrity — M0–M5 review fallout (in delivery)

- **Foreign keys + `Restrict` delete behavior added across all relational tables.** `PetConfiguration` / `OwnerConfiguration` / `UserConfiguration` / `AppointmentConfiguration` / `VaccinationConfiguration` declare explicit `HasOne<T>().WithMany().HasForeignKey(...)` relationships for `Pet → Owner`, `Pet/Owner/User → Tenant`, `Appointment → Pet`, `Appointment → User (VetUserId)`, `Vaccination → Pet`. Generated migration `20260508190409_AddForeignKeyConstraints` adds the seven FKs plus supporting indexes (`IX_pets_OwnerId`, `IX_appointments_PetId`, `IX_appointments_VetUserId`, `IX_vaccinations_PetId`).
- **Role-based authorization policies (`AdminOnly` / `VetOrAdmin` / `AnyStaff`).** New `src/VetCare.Api/Authorization/AuthorizationPolicies.cs` constants; `Program.cs` registers them with `AddAuthorizationBuilder().AddPolicy(...)` using `policy.RequireRole(...)` against the `ClaimTypes.Role` claim emitted by `JwtTokenService` (`Admin` / `Vet` / `Receptionist`).
- **Per-endpoint policy mapping.** Each minimal-API endpoint now calls `RequireAuthorization(<policy>)`: `GET /owners|pets|vaccinations|appointments` → `AnyStaff`; `POST/PUT/DELETE /owners|pets`, `POST/PUT /vaccinations`, `PUT /appointments/{id}/{confirm,complete}` → `VetOrAdmin`; `POST /appointments`, `PUT /appointments/{id}/cancel`, `POST /pets/{id}/photo` → `AnyStaff`. Group-level `RequireAuthorization()` is preserved so anonymous callers still get 401 (not 403).
- **`ScheduleAppointmentCommandHandler` validates `VetUserId`.** Loads the user via new `IRepository<User>` + `UserByIdSpec` (registered as `UserRepository : EfRepository<User>`); throws `NotFoundException` when the user is missing, belongs to a different tenant, or has `Role != UserRole.Vet`. The `User` tenant query filter already scopes the lookup, but the explicit `vet.TenantId != _tenantProvider.TenantId` check provides defense-in-depth.
- **Magic-byte sniffing on pet photo upload.** `PetEndpoints.UploadPetPhoto` reads the first 4 bytes of the form file and rejects with `400 "Invalid image file"` unless the prefix matches JPEG (`FF D8 FF`) or PNG (`89 50 4E 47`); stream `Position` is reset to `0` before the upload command runs. Content-type and 5 MB size checks remain as the first-line filters.

### Tests added

- Application unit tests: `ScheduleAppointmentHandlerTests` gains three cases (vet user not found / wrong tenant / wrong role) and the happy path now stubs `IRepository<User>` returning a `Vet` user — count 32 → 35.
- Integration tests: new `AuthorizationPolicyTests` (Receptionist `DELETE /owners` → 403, Receptionist `PUT /appointments/{id}/confirm` → 403) plus two new `PetPhotoEndpointTests` cases (valid PNG magic → 200; PDF bytes with `image/jpeg` content type → 400). `VetCareWebApplicationFactory` adds `CreateUserAsync` and `CreateUserAndIssueTokenAsync` helpers (scope-resolved `VetCareDbContext` + `IJwtTokenService`) so tests can seed a `Vet` user before scheduling and mint a Receptionist JWT for 403 assertions. Integration count 22 → 26.
- Gates: `make build` — 0/0; `make test` — 88 passing (Domain 27, Application 35, Integration 26); `make lint` — clean.

## fix/runtime-critical — M0–M5 review fallout (in delivery)

- **Vaccination list endpoint no longer 500s.** `ListVaccinationsQueryHandler` was missing despite `ListVaccinationsQuery`/validator/spec being wired through MediatR; added at `src/VetCare.Application/Vaccinations/Queries/ListVaccinations/ListVaccinationsQueryHandler.cs`, mirroring `ListAppointmentsQueryHandler` (paged + count specs over `IRepository<Vaccination>`, Mapster-projected to `VaccinationDto`).
- **LocalStack bootstrap aligned with runtime config.** `infra/localstack/init/01-bootstrap.sh` now creates `s3://vetcare-pets` (matches `S3:BucketName`) and the SQS queues `appointment-reminders` + `appointment-cancellations` (matches `QueueNames` constants used by `OnAppointmentScheduled` / `OnAppointmentCancelled`). The previous `vetcare-photos` / `vetcare-reminders` names did not match anything the app subscribes to.
- **Mongo dev connection string fixed.** `appsettings.Development.json` was passing `vetcare:vetcare@` credentials but `docker-compose.yml`'s `mongo:7` runs without auth, so the C# driver's SCRAM handshake failed. Switched the dev connection string to `mongodb://localhost:27017` to match the no-auth container; the integration test factory already used the same form.
- **Legacy status labels match the actual EF-stored enum.** `MonthlyReportGenerator.StatusLabels` had `Completed` / `Cancelled` swapped relative to `AppointmentStatus` (`Scheduled = 1, Confirmed = 2, Cancelled = 3, Completed = 4`); reordered to `Unknown / Scheduled / Confirmed / Cancelled / Completed` so int → label rendering is correct end-to-end (PDF text now reads "Cancelled" for status 3 and "Completed" for status 4). _Note: the original review prompt described a 0-based enum; verified against `src/VetCare.Domain/Appointments/AppointmentStatus.cs` and aligned with the actual 1-based values._
- **Legacy report is now tenant-scoped.** Added `tenantId` to `IAppointmentReportRepository.GetForMonth` / `EfAppointmentReportRepository` (SQL gains `WHERE a."TenantId" = @tenantId` plus an `Argument*` guard against `Guid.Empty`) and propagated through `MonthlyReportGenerator.Generate(tenantId, year, month, ...)`. `Program.Main` now requires `--tenant-id <guid>`; missing/invalid prints `Error: ...` + a usage line and returns exit 1.

### Tests added

- `tests/VetCare.Application.UnitTests/Vaccinations/ListVaccinationsHandlerTests.cs` — paged-result happy path (filter by `petId`, two items, total 5) and empty-list path. Application unit count 30 → 32.
- `tests/VetCare.LegacyReports.Tests/StatusLabelTests.cs` — `[Theory]` over `(1, "Scheduled")`, `(2, "Confirmed")`, `(3, "Cancelled")`, `(4, "Completed")` extracts the rendered PDF text via `iTextSharp.text.pdf.parser.PdfTextExtractor` and asserts the expected label is present.
- `tests/VetCare.LegacyReports.Tests/EfAppointmentReportRepositoryTests.cs` — reflects on the private `Sql` constant to assert it contains `a."TenantId" = @tenantId` (plus `@year` / `@month`); guards `GetForMonth` against `Guid.Empty` and out-of-range months. Legacy tests still build under `make build` and remain skipped at runtime by `IsTestProject=false` (per the M5 net48 + Linux note).
- Existing `MonthlyReportGeneratorTests` updated to pass a `Guid` tenant id through the new `Generate(tenantId, ...)` overload and the stub's `GetForMonth(tenantId, year, month)` signature.

### Gates

- `make build` — 0 warnings, 0 errors.
- `make test` — 80 passing (Domain 27, Application 32, Integration 21); legacy tests skipped at runtime by `IsTestProject=false`.
- `make lint` — clean.

## M5 — Legacy reports (.NET Framework 4.8 + EF6 + iTextSharp) (in delivery)

- `legacy/VetCare.LegacyReports` opts out of central package management (`<ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>`) so net48-only packages can pin their own versions without polluting `Directory.Packages.props` (which targets net8.0). Pinned: `EntityFramework` 6.5.1, `iTextSharp` 5.5.13.3, `Npgsql` 4.1.13. `Microsoft.NETFramework.ReferenceAssemblies` 1.0.3 keeps the project building on Linux without a Windows host.
- `App.config` carries the `DefaultConnection` connection string (Postgres, schema `vetcare`) and the `<supportedRuntime sku=".NETFramework,Version=v4.8">` startup section; `LegacyDbContext` reads it via `base("name=DefaultConnection")` and disables EF6 initializers (`Database.SetInitializer<LegacyDbContext>(null)`) so the read-only console never tries to migrate the existing schema.
- Flat read model `AppointmentReport` (`Id`, `TenantId`, `PetName`, `OwnerName`, `VetName`, `ScheduledAt`, `Status`, `VaccineName`) exposed as `DbSet<AppointmentReport>`. EF6 mapping pins it to `vetcare.appointments` with `Id` / `TenantId` / `ScheduledAt` / `Status` mapped 1:1; the joined columns are `Ignore`d so they're populated only via raw SQL.
- `EfAppointmentReportRepository` issues a single `Database.SqlQuery<AppointmentReport>` joining `appointments` → `pets` → `owners` → `users` and `LEFT JOIN`ing `vaccinations` on `(PetId, date_trunc('day', AdministeredAt))`. The month/year filter uses `EXTRACT(...)` with `NpgsqlParameter`s; results are materialized into an `IReadOnlyList<AppointmentReport>` for the report.
- `MonthlyReportGenerator` (constructor-injected `IAppointmentReportRepository`) writes a PDF via iTextSharp to `reports/vetcare-{year}-{month:D2}.pdf` (configurable output dir). Title `VetCare — Monthly Report {Month}/{Year}`, six-column table (Pet / Owner / Vet / Date / Status / Vaccine), an empty-state row when there are no appointments, and a per-page footer (`PdfPageEventHelper`) with `Generated at {UTC}` + page number.
- `Program.Main` parses `--year` / `--month` (case-insensitive, defaults to previous month based on `DateTime.UtcNow`), instantiates the context + repository + generator, prints `Report generated: {path}`, and returns `0` on success / `1` on caught exceptions (with the message routed to `Console.Error`).
- Status `int` is rendered through a small `StatusLabels` table (`Scheduled` / `Confirmed` / `Completed` / `Cancelled`) so the PDF stays readable without re-introducing a domain reference; `vet` is rendered as the user's email since the current `users` table has no display-name column.
- `tests/VetCare.LegacyReports.Tests` (net48, xUnit + FluentAssertions) drives `MonthlyReportGenerator` via a stubbed `IAppointmentReportRepository`: empty list → asserts the file exists at `vetcare-{yyyy}-{MM}.pdf` with a non-zero size and a `%PDF-` magic header; three rows → asserts each pet/owner/vaccine string is reachable through `iTextSharp.text.pdf.parser.PdfTextExtractor`, plus the title and the footer's `Generated at` marker. No real DB / SQLite needed.
- Test project sets `<IsTestProject>false</IsTestProject>` so `dotnet test VetCare.sln` skips it on Linux (VSTest has no working net48 host without mono); the project still **builds** under `make build` and the suite runs on Windows or via a mono-hosted xUnit console runner.
- Solution wired: `VetCare.sln` adds `tests/VetCare.LegacyReports.Tests` under the existing `tests` solution folder; nothing in `src/` (.NET 8 stack) references the legacy assembly.

### Gates

- `dotnet build legacy/VetCare.LegacyReports/VetCare.LegacyReports.csproj` — 0 warnings, 0 errors.
- `make build` — 0 warnings, 0 errors (full solution, including legacy + legacy tests on net48).
- `make lint` — clean (`dotnet format VetCare.sln --verify-no-changes`).
- `make test` — 78 passing (Domain 27, Application 30, Integration 21); legacy tests skipped at runtime by `IsTestProject=false` (net48 host requires mono).

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
