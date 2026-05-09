# Backlog

Future ideas and improvements deferred from M0–M5 and surfaced during the
quality/security review. Items here are **not committed scope** — they are
candidates for a future milestone or hardening pass.

## Testing & reliability

- **Testcontainers-backed integration tests for Postgres + Mongo.** ✅ Done in
  `feat/testcontainers`. `VetCareWebApplicationFactory` now spins up real
  `postgres:16` and `mongo:7` containers via `Testcontainers.PostgreSql` /
  `Testcontainers.MongoDb`, runs EF Core migrations on startup, and resolves
  the real `MongoAuditRepository` so the suite exercises the actual Npgsql
  provider (FKs, query filters, composite indexes) and the real Mongo driver.
- **LocalStack-backed S3 + SQS in integration tests.** Still pending. The
  factory keeps `IAmazonS3` / `ISqsPublisher` as NSubstitute fakes — bringing
  LocalStack into Testcontainers requires queue/bucket bootstrap, region
  wiring, and credential plumbing that does not pay off for current
  assertions. The Newman E2E CI job exercises the SQS publish path indirectly
  (the `Schedule appointment` step triggers `OnAppointmentScheduled`, which
  calls `ISqsPublisher` against the LocalStack queue), but it does **not** yet
  cover S3 — the pet-photo upload (`POST /api/v1/pets/{id}/photo`) is not in
  the collection. Adding a multipart-upload step plus a LocalStack S3
  round-trip assertion would close the gap.
- **Outbox pattern for domain-event reliability.** `VetCareDbContext.SaveChangesAsync`
  collects domain events and dispatches them via MediatR after commit. If the
  process dies between commit and publish, the SQS message is lost. Persist
  events in an `outbox` table inside the same transaction and have a worker
  drain them — one of the items the audit log decision (ADR-004) deliberately
  punted on.

## Authentication & authorization

- **Rate limiting + lockout on auth endpoints.** `POST /auth/login` and
  `POST /auth/register` are unprotected against credential stuffing / brute
  force. Add ASP.NET Core rate limiting (per-IP and per-account) and a soft
  lockout after N failures.
- **`AdminOnly` policy endpoints.** The `AdminOnly` policy was added in
  `fix/security-integrity` but no endpoint requires it yet. Define and protect
  admin operations (tenant management, role assignment, user enable/disable)
  before the policy can be considered exercised.
- **Exhaustive 403 role coverage.** `AuthorizationPolicyTests` covers two
  representative cases (Receptionist `DELETE /owners`, Receptionist `PUT
  /appointments/{id}/confirm`). Extend to a `[Theory]` that walks every
  endpoint × role combination so future endpoint additions don't silently
  ship without policy coverage.

## Domain modelling

- **Domain events with stable `EventId` / `OccurredOn`.** Events currently
  carry only their domain payload. Adding `Guid EventId { get; }` and
  `DateTime OccurredOn { get; }` (set in the event constructor) is a
  prerequisite for the outbox above and for downstream idempotency.
- **Clean Architecture: remove EF DbSet from Application interface.** The
  `IRepository<T>` abstraction is clean, but a few Application-layer touch
  points still surface EF concepts indirectly (e.g. `IQueryable` in the spec
  evaluator is reachable from Application). Audit and tighten so that
  Application has zero EF Core symbol references — only the abstraction and
  the spec contract.
