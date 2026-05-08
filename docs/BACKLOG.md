# Backlog

Future ideas and improvements deferred from M0–M5 and surfaced during the
quality/security review. Items here are **not committed scope** — they are
candidates for a future milestone or hardening pass.

## Testing & reliability

- **Testcontainers-backed integration tests for Postgres, Mongo, and LocalStack.**
  The current integration suite uses `WebApplicationFactory` with EF in-memory
  / NSubstitute fakes for `IAmazonS3` / `IAmazonSQS` / `IMongoClient`. Replace
  the fakes with Testcontainers so the suite exercises the real Npgsql provider
  (catches query-filter / EF-translation bugs), the real Mongo driver against a
  real database, and LocalStack for S3 + SQS round-trips.
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
