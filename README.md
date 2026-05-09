# VetCare

Multi-tenant SaaS for veterinary clinics — tutors, pets, appointments, vaccinations, and medical records — built as a portfolio-grade demonstration of Clean Architecture, multi-tenancy, and operational maturity in a modern .NET stack.

[![CI](https://github.com/paulo-raoni/vetcare/actions/workflows/ci.yml/badge.svg)](https://github.com/paulo-raoni/vetcare/actions/workflows/ci.yml)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](#license)

---

## Table of contents

1. [Overview](#1-overview)
2. [Architecture](#2-architecture)
3. [Project structure](#3-project-structure)
4. [Tech stack](#4-tech-stack)
5. [Getting started](#5-getting-started)
6. [API reference](#6-api-reference)
7. [Key design decisions](#7-key-design-decisions)
8. [Legacy module](#8-legacy-module)
9. [CI/CD](#9-cicd)
10. [Backlog](#10-backlog)
11. [License](#11-license)

---

## 1. Overview

VetCare models the day-to-day workflow of a veterinary clinic — registering tutors, their pets, scheduling appointments with veterinarians, and recording vaccinations — across **multiple tenants** sharing the same deployment. It is a portfolio project, not a production product: every milestone is built to surface the decisions a mid-to-senior engineer makes in a real .NET / pet-health SaaS.

**Technical highlights:**

- **Clean Architecture** on .NET 8 — Domain / Application / Infrastructure / Api with one-way dependencies enforced by project references.
- **CQRS via MediatR** with a `ValidationBehavior` (FluentValidation) and `AuditBehavior` pipeline.
- **Multi-tenancy** through EF Core global query filters bound to `ITenantProvider` — single point of isolation enforcement.
- **JWT bearer + BCrypt** authentication with role-based policies (`AdminOnly`, `VetOrAdmin`, `AnyStaff`).
- **Domain events** dispatched after `SaveChangesAsync` and fanned out to **AWS SQS** for asynchronous consumers.
- **MongoDB audit log** capturing every successful command (action, tenant, user, payload).
- **AWS S3** photo storage with magic-byte sniffing on upload.
- **Specification pattern** over a generic `IRepository<T>` for composable, testable queries.
- **Legacy .NET Framework 4.8 + EF6 + iTextSharp** console that emits monthly PDF reports against the same Postgres schema — demonstrates working with brownfield .NET.
- **React 18 + TypeScript** frontend (M6, in progress).

---

## 2. Architecture

VetCare follows Clean Architecture: dependencies point inward, the domain has no infrastructure concerns, and the API project is the only composition root.

```
 ┌──────────────────────────────────────────────────────────────────┐
 │  VetCare.Api                  (composition root, ASP.NET Core)   │
 │  Minimal-API endpoints · DI wiring · JWT middleware · Swagger    │
 └───────────────────────────────┬──────────────────────────────────┘
                                 │ references
                                 ▼
 ┌──────────────────────────────────────────────────────────────────┐
 │  VetCare.Infrastructure       (adapters)                         │
 │  EF Core 8 / Npgsql · MongoDB driver · AWS S3 / SQS · JWT issuer │
 │  EfRepository<T> · SpecificationEvaluator<T>                     │
 └───────────────────────────────┬──────────────────────────────────┘
                                 │ references
                                 ▼
 ┌──────────────────────────────────────────────────────────────────┐
 │  VetCare.Application          (use cases)                        │
 │  MediatR commands & queries · FluentValidation validators        │
 │  Mapster mappings · IRepository<T> / ISpecification<T> contracts │
 │  ValidationBehavior · AuditBehavior pipelines                    │
 └───────────────────────────────┬──────────────────────────────────┘
                                 │ references
                                 ▼
 ┌──────────────────────────────────────────────────────────────────┐
 │  VetCare.Domain               (pure)                             │
 │  Aggregates: Tenant, User, Owner, Pet, Appointment, Vaccination  │
 │  Domain events · invariants · value objects                      │
 │  Zero external dependencies                                      │
 └──────────────────────────────────────────────────────────────────┘

      ── isolated ──────────────────────────────────────────────────
       legacy/VetCare.LegacyReports  (.NET Framework 4.8 + EF6)
       Reads the same PostgreSQL schema via raw SQL; never
       referenced by any .NET 8 project.
```

**Layer responsibilities:**

- **Domain** — aggregates, invariants, domain events. No external references; unit-testable without a host.
- **Application** — use cases as MediatR handlers, validators, DTOs, repository/spec abstractions, cross-cutting pipelines (validation, audit). Depends only on Domain.
- **Infrastructure** — concrete adapters: EF Core/Postgres, MongoDB audit, S3 storage, SQS publisher, JWT issuer, BCrypt hasher. Depends on Application + Domain.
- **Api** — ASP.NET Core minimal-API endpoints, DI wiring, JWT/auth middleware, Swagger. The only project that knows about every other.

**Legacy isolation.** `legacy/VetCare.LegacyReports` is `net48` with central package management opted out; nothing in `src/` references it, and it builds on a Windows runner only.

---

## 3. Project structure

```
.
├── src/
│   ├── VetCare.Domain/              Aggregates, domain events, invariants. No deps.
│   ├── VetCare.Application/         MediatR handlers, validators, DTOs, abstractions.
│   ├── VetCare.Infrastructure/      EF Core, Mongo, S3/SQS, JWT, BCrypt adapters.
│   └── VetCare.Api/                 ASP.NET Core entry point + minimal-API endpoints.
├── tests/
│   ├── VetCare.Domain.UnitTests/            Aggregate invariants and events.
│   ├── VetCare.Application.UnitTests/       Handlers, validators, pipeline behaviors.
│   ├── VetCare.Infrastructure.IntegrationTests/   API tests via WebApplicationFactory.
│   └── VetCare.LegacyReports.Tests/         net48 PDF rendering + raw-SQL guards.
├── legacy/
│   └── VetCare.LegacyReports/       .NET Framework 4.8 + EF6 + iTextSharp PDF console.
├── client/
│   └── vetcare-web/                 React 18 + TypeScript + Vite (M6, in progress).
├── docs/
│   ├── PROGRESS.md                  Per-milestone delivery log.
│   ├── DECISIONS.md                 Architectural decision records (ADR-001 … ADR-008).
│   ├── BACKLOG.md                   Deferred ideas — explicitly not committed scope.
│   ├── decisions/                   Per-decision detail files.
│   └── e2e/                         Newman/Postman collection + e2e README.
├── infra/
│   └── localstack/init/             LocalStack S3 bucket + SQS queue bootstrap.
├── .github/workflows/               CI (build, test, lint) + lint pipelines.
├── Dockerfile                       Multi-stage build, non-root, healthcheck on /health.
├── docker-compose.yml               Postgres 16 + Mongo 7 + LocalStack 3.
└── Makefile                         up / down / build / test / lint / docker-build.
```

---

## 4. Tech stack

| Layer       | Technology                                                                 |
| ----------- | -------------------------------------------------------------------------- |
| API         | ASP.NET Core 8, Minimal APIs, `Asp.Versioning`, Swashbuckle (OpenAPI)      |
| ORM         | EF Core 8 + Npgsql; Specification pattern over `IRepository<T>`            |
| Database    | PostgreSQL 16 (`vetcare` schema)                                           |
| Cache       | Not used (in-memory `IMemoryCache` only when needed; Redis is backlog)     |
| Queue       | Amazon SQS (LocalStack in dev) via `IAmazonSQS` + `ISqsPublisher`          |
| Storage     | Amazon S3 (LocalStack in dev) via `IAmazonS3` + `IStorageService`          |
| Audit       | MongoDB 7 (`audit_log` collection) via `MongoAuditRepository`              |
| Auth        | JWT bearer (HS256), BCrypt password hashes, role-based policies            |
| Tests       | xUnit, FluentAssertions, NSubstitute, `WebApplicationFactory`              |
| Legacy      | .NET Framework 4.8 + EF6 + iTextSharp (PDF) + Npgsql 4                     |
| CI          | GitHub Actions: `ci.yml` (build/test/lint) + `lint.yml` + Windows legacy build |

---

## 5. Getting started

### Prerequisites

- Docker + Docker Compose
- .NET 8 SDK (`dotnet --version` ≥ `8.0.x`, see [`global.json`](./global.json))
- Node 20+ (only required for the React frontend in M6)
- `dotnet-ef` global tool: `dotnet tool install --global dotnet-ef`

### Running locally

1. **Clone**

   ```bash
   git clone https://github.com/paulo-raoni/vetcare.git
   cd vetcare
   ```

2. **Configure environment.** Copy the placeholder file and adjust as needed:

   ```bash
   cp .env.example .env
   ```

   The local-dev defaults already in `appsettings.Development.json` work out of the box for the Docker Compose services in step 3 — `.env` only matters when you want to override values (production, alternate hosts, real AWS credentials).

3. **Start Postgres, Mongo, and LocalStack**

   ```bash
   make up
   ```

   The `infra/localstack/init/01-bootstrap.sh` script creates the `vetcare-pets` S3 bucket and the `appointment-reminders` / `appointment-cancellations` SQS queues automatically.

4. **Apply EF Core migrations**

   ```bash
   dotnet ef database update \
     --project src/VetCare.Infrastructure \
     --startup-project src/VetCare.Api
   ```

5. **Run the API on port 5000** (matches the e2e collection)

   ```bash
   dotnet run --project src/VetCare.Api --urls http://localhost:5000
   ```

6. **Open Swagger** → <http://localhost:5000/swagger>

### Running tests

```bash
make test
```

| Project                                       | Coverage                                                                         |
| --------------------------------------------- | -------------------------------------------------------------------------------- |
| `VetCare.Domain.UnitTests`                    | Aggregate invariants, state machines, domain-event raising.                      |
| `VetCare.Application.UnitTests`               | MediatR handlers (happy/error paths), validators, `ValidationBehavior` + `AuditBehavior`. |
| `VetCare.Infrastructure.IntegrationTests`     | Integration tests via `WebApplicationFactory` backed by real PostgreSQL and MongoDB containers (Testcontainers). Covers register → JWT → CRUD → photo upload → 401/403/409 paths. |
| `VetCare.LegacyReports.Tests`                 | iTextSharp PDF rendering + raw-SQL tenant guards (skipped on Linux runtime; built in CI on Windows). |
| `E2E (Newman)`                                | Full happy-path flow via Newman against a live API instance (CI job: `e2e`). |

### Running E2E (Newman)

The Postman/Newman collection at [`docs/e2e/vetcare.collection.json`](./docs/e2e/vetcare.collection.json) walks the full happy path: `register → login → owner → pet → appointment → confirm → vaccination`. See [`docs/e2e/README.md`](./docs/e2e/README.md) for full details.

Prerequisites: API running on `http://localhost:5000`, Postgres up and migrated, [Newman](https://www.npmjs.com/package/newman) installed (`npm install -g newman`).

```bash
# default (http://localhost:5000)
newman run docs/e2e/vetcare.collection.json --env-var baseUrl=http://localhost:5000

# alternate host
newman run docs/e2e/vetcare.collection.json --env-var baseUrl=https://staging.vetcare.example.com
```

A successful run reports `Passed` for every assertion and exits `0`.

---

## 6. API reference

All routes are prefixed with `/api/v1`. Authenticated endpoints expect a `Bearer <jwt>` header issued by `POST /auth/login` (or `POST /auth/register` on first use). Roles map to the `Admin` / `Vet` / `Receptionist` claims minted by `JwtTokenService`.

### Auth

| Method | Path                | Auth      | Role | Description                                          |
| ------ | ------------------- | --------- | ---- | ---------------------------------------------------- |
| POST   | `/auth/register`    | Anonymous | —    | Provision a new tenant + admin user; returns a JWT.  |
| POST   | `/auth/login`       | Anonymous | —    | Authenticate against `(tenantSlug, email, password)`. |

### Users

| Method | Path             | Auth   | Role        | Description                                                  |
| ------ | ---------------- | ------ | ----------- | ------------------------------------------------------------ |
| POST   | `/users`         | Bearer | `AdminOnly` | Create a new user within the tenant (email, password, role). |

### Owners

| Method | Path                | Auth   | Role         | Description                       |
| ------ | ------------------- | ------ | ------------ | --------------------------------- |
| GET    | `/owners`           | Bearer | `AnyStaff`   | List owners (paginated).          |
| GET    | `/owners/{id}`      | Bearer | `AnyStaff`   | Get an owner by id.               |
| POST   | `/owners`           | Bearer | `VetOrAdmin` | Create a new owner.               |
| PUT    | `/owners/{id}`      | Bearer | `VetOrAdmin` | Update an owner.                  |
| DELETE | `/owners/{id}`      | Bearer | `VetOrAdmin` | Delete an owner.                  |

### Pets

| Method | Path                  | Auth   | Role         | Description                                  |
| ------ | --------------------- | ------ | ------------ | -------------------------------------------- |
| GET    | `/pets`               | Bearer | `AnyStaff`   | List pets (paginated, filterable by `ownerId`). |
| GET    | `/pets/{id}`          | Bearer | `AnyStaff`   | Get a pet by id.                             |
| POST   | `/pets`               | Bearer | `VetOrAdmin` | Create a new pet.                            |
| PUT    | `/pets/{id}`          | Bearer | `VetOrAdmin` | Update a pet.                                |
| DELETE | `/pets/{id}`          | Bearer | `VetOrAdmin` | Delete a pet.                                |
| POST   | `/pets/{id}/photo`    | Bearer | `AnyStaff`   | Upload a pet photo (`multipart/form-data`, ≤ 5 MB, `image/jpeg` or `image/png`). |

### Appointments

| Method | Path                                | Auth   | Role         | Description                                                 |
| ------ | ----------------------------------- | ------ | ------------ | ----------------------------------------------------------- |
| GET    | `/appointments`                     | Bearer | `AnyStaff`   | List appointments (filterable by `petId`, `status`, date range). |
| GET    | `/appointments/{id}`                | Bearer | `AnyStaff`   | Get an appointment by id.                                   |
| POST   | `/appointments`                     | Bearer | `AnyStaff`   | Schedule a new appointment (validates vet user role + tenant). |
| PUT    | `/appointments/{id}/confirm`        | Bearer | `VetOrAdmin` | `Scheduled → Confirmed` transition.                         |
| PUT    | `/appointments/{id}/cancel`         | Bearer | `AnyStaff`   | Cancel from `Scheduled` or `Confirmed`.                     |
| PUT    | `/appointments/{id}/complete`       | Bearer | `VetOrAdmin` | `Confirmed → Completed` transition.                         |

Illegal transitions return **409 Conflict**.

### Vaccinations

| Method | Path                       | Auth   | Role         | Description                                   |
| ------ | -------------------------- | ------ | ------------ | --------------------------------------------- |
| GET    | `/vaccinations`            | Bearer | `AnyStaff`   | List vaccinations (filterable by `petId`).    |
| GET    | `/vaccinations/{id}`       | Bearer | `AnyStaff`   | Get a vaccination by id.                      |
| POST   | `/vaccinations`            | Bearer | `VetOrAdmin` | Record a vaccination (`administeredAt` not in the future, `nextDueAt ≥ administeredAt`). |
| PUT    | `/vaccinations/{id}`       | Bearer | `VetOrAdmin` | Update a vaccination record.                  |

Anonymous calls to authenticated endpoints get **401**; authenticated calls without the required role get **403**. See [`docs/DECISIONS.md`](./docs/DECISIONS.md#adr-007-jwt-bearer--bcrypt-for-authentication) for the auth model.

---

## 7. Key design decisions

The five most consequential decisions, summarised. Full ADRs (with context, decision, and consequences) are in [`docs/DECISIONS.md`](./docs/DECISIONS.md).

**ADR-001 — Clean Architecture layer separation.** Four projects with one-way references: `Domain` (no deps), `Application` → Domain, `Infrastructure` → Application + Domain, `Api` references all three. Dependency rules are enforced by project references rather than analyzers. The cost is mild boilerplate for end-to-end features; the payoff is a domain that is unit-testable without a host and infrastructure concerns that never leak upward.

**ADR-002 — MediatR + CQRS for use cases.** Each use case is one handler in its own folder; commands implement an `ICommand` marker (used by `AuditBehavior`) so audit and validation pipelines apply uniformly without per-handler plumbing. Queries are trivially distinguishable from commands, which makes the no-tracking split (commands keep tracking, queries call `AsNoTracking`) and audit-payload exclusion straightforward.

**ADR-003 — Multi-tenancy via EF Core global query filters.** `ITenantProvider` is resolved from the JWT and bound into `VetCareDbContext`; every `ITenantEntity` carries a global filter `e.TenantId == _tenant.TenantId`. New aggregates only need to implement `ITenantEntity` to be tenant-scoped — there are no per-handler `Where(x => x.TenantId == ...)` calls to forget. The trade-off: raw SQL bypasses the filter, which is why the legacy EF6 console takes `tenantId` as an explicit argument.

**ADR-004 — MongoDB for the audit log.** Audit entries are write-heavy and schema-flexible (payload shape varies per command); pushing them into the relational schema would force a migration on every payload change. `AuditBehavior` captures successful commands into MongoDB through `IAuditRepository`; failures inside the audit store are swallowed with a warning so the request never fails because audit is down.

**ADR-006 — SQS for asynchronous appointment events.** Domain events (`AppointmentScheduledEvent`, `AppointmentCancelledEvent`) are dispatched after `SaveChangesAsync` and fanned out to `appointment-reminders` / `appointment-cancellations` via `ISqsPublisher`. This decouples the API request from notification side-effects. The known reliability gap — publish is post-commit but not transactional — is tracked as the *outbox pattern* item in [`docs/BACKLOG.md`](./docs/BACKLOG.md).

---

## 8. Legacy module

`legacy/VetCare.LegacyReports` is a **.NET Framework 4.8** console app that emits monthly PDF reports (`Pet`, `Owner`, `Vet`, `Date`, `Status`, `Vaccine`) directly from the same PostgreSQL schema via **Entity Framework 6** raw SQL and **iTextSharp 5**.

```bash
dotnet run --project legacy/VetCare.LegacyReports -- \
  --tenant-id <guid> --year 2026 --month 5
```

Output: `reports/vetcare-2026-05.pdf`. Missing or invalid arguments print a usage line and exit `1`.

**Why it exists.** Real .NET shops carry brownfield code: legacy scheduled jobs, EF6 contexts, iTextSharp PDFs, central package management exemptions, and Linux/Windows build asymmetry. This module demonstrates working with those constraints — central package management is opted out for `net48`, the project never references the .NET 8 stack, the test project sets `<IsTestProject>false</IsTestProject>` so `dotnet test` skips it on Linux (no mono host), and the GitHub Actions pipeline includes a dedicated Windows job to compile it. The report is **tenant-scoped** end-to-end (raw SQL takes a `tenantId` parameter) because the multi-tenant query filter doesn't apply to raw SQL.

---

## 9. CI/CD

Two GitHub Actions workflows in [`.github/workflows/`](./.github/workflows/):

**`ci.yml`** — runs on `push` to `main` and on every pull request to `main`:

- `build-and-test` (Ubuntu): `dotnet restore` → `dotnet build -c Release` → `dotnet test -c Release` → `dotnet format --verify-no-changes`.
- `legacy-build` (Windows): `dotnet build legacy/VetCare.LegacyReports/VetCare.LegacyReports.csproj -c Release` so the net48 module never falls behind silently.
- `e2e` (Ubuntu, runs after `build-and-test`): boots the full stack via `docker compose up postgres mongo localstack`, applies EF Core migrations, runs the API on port 5000, polls `/health` until ready, then executes [`docs/e2e/vetcare.collection.json`](./docs/e2e/vetcare.collection.json) with `newman run` to verify the register → vet user → owner → pet → appointment → confirm → vaccination flow against real services. Containers are torn down with `docker compose down -v` even on failure.

**`lint.yml`** — runs on every pull request: `dotnet format VetCare.sln --verify-no-changes` (style-only check, fails fast on whitespace/formatting drift).

**Gates enforced on every PR:**

```bash
make build      # dotnet build VetCare.sln
make test       # dotnet test VetCare.sln
make lint       # dotnet format VetCare.sln --verify-no-changes
```

A delivery where any gate fails is not a delivery. Every PR must pass all three gates before merge.

---

## 10. Backlog

Deferred ideas and improvements — not committed scope. See [`docs/BACKLOG.md`](./docs/BACKLOG.md) for the full list.

- **LocalStack-backed S3 + SQS in integration tests.** Postgres + Mongo are now real containers via Testcontainers, but `IAmazonS3` / `ISqsPublisher` remain NSubstitute fakes. Bringing LocalStack into Testcontainers (queue + bucket bootstrap, region/credential wiring) would close the last gap; the Newman E2E job already exercises the real drivers end-to-end against docker-compose.
- **Outbox pattern for domain-event reliability.** Persist domain events in an `outbox` table inside the same transaction as the aggregate write, then drain to SQS via a worker — closes the post-commit-publish gap noted in ADR-006.
- **Rate limiting + lockout on auth endpoints.** Add ASP.NET Core rate limiting (per-IP and per-account) and a soft lockout after N failures on `POST /auth/login` and `POST /auth/register`.

---

## 11. License

Released under the [MIT License](./LICENSE).
