# Decision Log

Architectural decisions made during M0–M5. Each entry follows the lightweight ADR
template (Context / Decision / Consequences). New entries append to the end and
keep monotonically increasing IDs.

---

### [ADR-001] Clean Architecture layer separation

- **Date:** 2026-05-07
- **Status:** Accepted

**Context.** VetCare needs to demonstrate the separation a mid-to-senior engineer
would draw in a real .NET SaaS, where the domain model must remain testable and
free of infrastructure concerns. The portfolio audience expects an explicit
layered structure rather than a single-project app.

**Decision.** Split the solution into four projects with strict dependency rules:
`VetCare.Domain` (no external references), `VetCare.Application` (depends on
Domain only), `VetCare.Infrastructure` (depends on Application + Domain), and
`VetCare.Api` (composition root, references all three). The rules are stated in
the project conventions and enforced by project references rather than analyzers.

**Consequences.** Domain stays pure and unit-testable without a host. Cross-cutting
concerns (EF, Mongo, AWS) live in Infrastructure, never leak upward. The Api
project is the only place that performs DI wiring. The cost is mild boilerplate
for small features that span every layer.

---

### [ADR-002] MediatR + CQRS for use cases

- **Date:** 2026-05-07
- **Status:** Accepted

**Context.** The Application layer needs a uniform shape for use cases, request
validation, cross-cutting behaviors (validation, audit), and domain-event
dispatch. Reaching for an ad-hoc service-class pattern would obscure the read /
write split and make pipeline behaviors awkward to add later.

**Decision.** Use MediatR with an explicit CQRS split: commands implement
`IRequest<T>` and a marker `ICommand` (used by `AuditBehavior`); queries
implement `IRequest<T>` only. Each use case is one handler class in its own
folder. `ValidationBehavior` and `AuditBehavior` are registered as pipeline
behaviors in `Application/DependencyInjection.cs`.

**Consequences.** Validation and auditing apply uniformly without per-handler
plumbing. Queries are trivially distinguishable from commands, which makes the
no-tracking split (see ADR-008) and audit-payload exclusion trivial. Trade-off:
runtime indirection through `IMediator` makes call graphs slightly harder to
follow in IDE search.

---

### [ADR-003] Multi-tenancy via EF Core global query filters

- **Date:** 2026-05-07
- **Status:** Accepted

**Context.** VetCare is a multi-tenant SaaS: every clinic's data must be isolated
without per-query `Where(x => x.TenantId == ...)` calls scattered across
handlers. Forgetting one is a cross-tenant data leak.

**Decision.** Implement `ITenantProvider` (resolved from the JWT in
`CurrentUserService`), bind it into `VetCareDbContext`, and apply a global query
filter `e.TenantId == _tenant.TenantId` on every `ITenantEntity`. All read paths
pick this up automatically; command handlers stamp `TenantId` on creation.

**Consequences.** Single point of enforcement; new aggregates only need
`ITenantEntity` to be tenant-scoped. The filter cooperates with `AsNoTracking`
and Specifications. Risks: an `IgnoreQueryFilters()` slip would bypass
isolation, so direct calls to it are reviewed in PRs. The filter cannot guard
raw SQL — the legacy EF6 reporting path (M5) had to take `tenantId` explicitly
(see `fix/runtime-critical`).

---

### [ADR-004] MongoDB for the audit log

- **Date:** 2026-05-07
- **Status:** Accepted

**Context.** Audit entries are write-heavy, schema-flexible (payload shape
varies per command), and read on demand for forensics — not on the request hot
path. Forcing them into the relational schema would couple every command to a
strict audit table and cost a migration on every payload-shape change.

**Decision.** Persist `AuditEntry` (`Id`, `TenantId?`, `UserId?`, `Action`,
`EntityType?`, `EntityId?`, `Payload`, `OccurredAt`) to MongoDB through
`IAuditRepository` / `MongoAuditRepository`. `AuditBehavior<TRequest, TResponse>`
captures successful command executions; failures inside the audit store are
swallowed with a warning so the request never fails because audit is down.

**Consequences.** Schema-flexible payload via `BsonDocument.Parse`. The audit
collection is independent of the relational schema and can be archived or
sharded separately. Trade-off: a second datastore in dev (Mongo container in
`docker-compose.yml`) and a second connection-string surface in
`appsettings.Development.json`.

---

### [ADR-005] S3 for pet photo storage

- **Date:** 2026-05-07
- **Status:** Accepted

**Context.** Pet photos are user-uploaded binary content (~MB-scale). Storing
them in PostgreSQL would bloat the relational backups and tightly couple the
DB to delivery latency. The portfolio target is a real cloud object store.

**Decision.** Define `IStorageService` in Application; implement
`S3StorageService` over `IAmazonS3` in Infrastructure. Keys are namespaced
`pets/{tenantId}/{petId}/{guid}{ext}`. LocalStack is used in dev (path-style
URLs); real S3 in prod (virtual-host URLs). Magic-byte sniffing on upload
(`fix/security-integrity`) keeps the bucket free of mis-typed payloads.

**Consequences.** Photos are decoupled from the relational backup story and can
be served via CDN later. The bucket name is configuration
(`S3:BucketName`); the LocalStack init script must mirror it (cf.
`fix/runtime-critical`). Trade-off: integration tests must substitute
`IAmazonS3` to avoid hitting LocalStack from the test factory.

---

### [ADR-006] SQS for asynchronous appointment events

- **Date:** 2026-05-07
- **Status:** Accepted

**Context.** Appointment scheduling and cancellation should fan out to
notification consumers (reminders, audit, future integrations) without coupling
the API request to those side-effects. In-process events are sufficient for now
but a message broker is a more honest demonstration of the pattern.

**Decision.** Define `ISqsPublisher` in Application with `QueueNames` constants;
implement `SqsPublisher` over `IAmazonSQS` in Infrastructure (cached
`GetQueueUrl`). MediatR `INotificationHandler`s `OnAppointmentScheduled` and
`OnAppointmentCancelled` publish JSON `AppointmentReminderMessage` payloads to
`appointment-reminders` and `appointment-cancellations`.

**Consequences.** Domain events flow from the aggregate through the dispatcher
in `SaveChangesAsync` → SQS, decoupling consumers. Same caveat as ADR-005:
queue names live in `QueueNames` and the LocalStack init script must mirror
them. Reliability gap: the publish is post-commit but not transactional — see
the outbox pattern entry in `BACKLOG.md`.

---

### [ADR-007] JWT bearer + BCrypt for authentication

- **Date:** 2026-05-07
- **Status:** Accepted

**Context.** Authentication needs to be self-contained (no external IdP for a
portfolio project) yet show industry-standard primitives: hashed passwords,
short-lived bearer tokens, role claims. Sessions in a database or cookies would
add scope without adding signal.

**Decision.** Hash passwords with BCrypt (`BCrypt.Net.Next`), issue HS256 JWTs
through `IJwtTokenService`, and configure `JwtBearer` via `IOptions<JwtOptions>`
so tests can override the signing key. The token carries `sub`, `tenant_id`,
and `role` claims; `CurrentUserService` projects the claims into
`ITenantProvider` and the role-based authorization policies (`AdminOnly`,
`VetOrAdmin`, `AnyStaff`) added in `fix/security-integrity`.

**Consequences.** Auth is end-to-end testable without any external dependency.
Role policies are enforced per-endpoint and unit-coverable via
`AuthorizationPolicyTests`. Trade-off: JWT revocation requires either a short
TTL or a deny-list — neither is in scope for the portfolio (see backlog
entries on rate limiting / lockout).

---

### [ADR-008] Specification pattern for repository queries

- **Date:** 2026-05-07
- **Status:** Accepted

**Context.** Handlers need composable, testable read queries (filter, include,
order, page). Exposing `IQueryable<T>` directly leaks EF semantics into
Application; building one repository method per query bloats the interface and
duplicates filtering logic.

**Decision.** Introduce `ISpecification<T>` / `Specification<T>` in
`Application.Abstractions.Specifications` and a generic `IRepository<T>` with
`SingleOrDefaultAsync(spec)` / `ListAsync(spec)` / `CountAsync(spec)`.
Concrete specs (`OwnerListSpec`, `AppointmentListSpec`, etc.) declare criteria,
includes, ordering, and paging. `EfRepository<T>` evaluates them through
`SpecificationEvaluator<T>`.

**Consequences.** Handlers stay free of EF; new query shapes are one new spec
class. Specs are unit-testable because they expose criteria/order/paging as
data. Combined with the no-tracking split in `fix/quality`, list and ById
queries skip change-tracker overhead while command handlers keep tracked
loads. Trade-off: complex projections (e.g. cross-aggregate joins for
reporting) still need a non-spec escape hatch — the legacy EF6 console
sidesteps the spec layer entirely.

---

### [ADR-009] Transactional outbox for domain-event delivery

- **Date:** 2026-05-10
- **Status:** Accepted

**Context.** ADR-006 introduced SQS as the asynchronous transport for
appointment events but explicitly recorded a reliability gap: the publish is
post-commit but not transactional. `VetCareDbContext.SaveChangesAsync` collects
domain events from tracked aggregates, commits the EF transaction, and only
then dispatches via MediatR `IPublisher` — so a process crash between commit
and publish drops the SQS message, with no way to recover the lost
notification. The existing audit log (ADR-004) does not mitigate this; it
captures successful command executions, not domain events. The portfolio
target is at-least-once delivery without weakening the existing tenant
isolation, idempotency stance, or in-process notification handlers.

**Decision.** Promote the outbox follow-up into milestone M8: persist domain
events as `OutboxMessage` rows in the same EF transaction as the originating
aggregate change, and drain the table with a hosted `OutboxProcessor` that
calls the in-process `IPublisher`. `IDomainEvent` gains stable
`Guid EventId` and `DateTime OccurredOnUtc` properties (constructor-set,
replacing the unstable default-implemented getters) so events round-trip
through `jsonb` storage with consistent identity. The cutover is staged across
four PRs: metadata change, dual-write, worker, and removal of the post-commit
publish — see `docs/decisions/M8.md` for the full data model, worker
semantics, and test plan.

**Consequences.** At-least-once delivery becomes a CI-verifiable property via
a Testcontainers Postgres test that simulates a publisher failure across the
commit boundary. The `(ProcessedOnUtc, OccurredOnUtc)` index keeps the
worker's batch scan cheap; `SELECT ... FOR UPDATE SKIP LOCKED` makes the
design safe for multi-instance deploys. The `IDomainEvent` shape change is
breaking for any external producer of these events (there are none today).
Trade-offs: idempotency moves to the consumer side (documented), poison rows
accumulate until a future Mongo-audited dead-letter table is added, and the
worker shares the request-path Postgres instance — pressure to be revisited if
integration timing surfaces contention.
