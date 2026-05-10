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
- **Outbox pattern for domain-event reliability.** ✅ Delivered in M8
  (four PRs: `feat/m8-outbox-domain-event-metadata` →
  `feat/m8-outbox-table-and-write-path` → `feat/m8-outbox-worker` →
  `feat/m8-outbox-cutover`). The reliability gap ADR-006 documented
  is closed: events are written to `outbox_messages` in the same EF
  transaction as the aggregate change and drained by an
  `OutboxProcessor : BackgroundService` with persistent exponential
  backoff (1s base, 30s ceiling), poison-row exclusion at
  `MaxAttempts`, and multi-instance safety via
  `SELECT ... FOR UPDATE SKIP LOCKED`. See ADR-009 in
  `docs/DECISIONS.md` and the implementation summary in
  `docs/decisions/M8.md`.
- **Mongo-audited dead-letter table for outbox poison rows.** When an
  outbox row reaches `MaxAttempts` it is excluded from future polls
  and warning-logged, but the row stays in `outbox_messages`
  indefinitely with no inspection path. A future milestone could
  move poison rows to a dedicated `outbox_poison` table with the
  failure history and surface them through the Mongo audit log so
  operators can replay or discard them deliberately. Natural
  continuation of the M8 work.

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

- **Domain events with stable `EventId` / `OccurredOn`.** ✅ Delivered as
  PR (1) of M8. `IDomainEvent` now exposes abstract `EventId` and
  `OccurredOnUtc` (renamed from `OccurredOn` to make the UTC contract
  explicit) with `init`-set implementations on the six event records,
  so values are stable across reads and round-trippable through
  `System.Text.Json` for outbox storage.
- **Clean Architecture: remove EF DbSet from Application interface.** The
  `IRepository<T>` abstraction is clean, but a few Application-layer touch
  points still surface EF concepts indirectly (e.g. `IQueryable` in the spec
  evaluator is reachable from Application). Audit and tighten so that
  Application has zero EF Core symbol references — only the abstraction and
  the spec contract.
