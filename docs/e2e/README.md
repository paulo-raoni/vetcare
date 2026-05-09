# VetCare — E2E HTTP test suite (Newman)

This directory contains a Postman/Newman collection that exercises the full
VetCare happy path against a running API instance.

## Collection

[`vetcare.collection.json`](./vetcare.collection.json) — Postman v2.1 collection
covering the request sequence:

1. `POST /api/v1/auth/register` — provision a fresh tenant + admin user
2. `POST /api/v1/auth/login` — re-authenticate and capture the JWT
3. `POST /api/v1/users` — create a `Vet` user inside the same tenant (admin-only); the appointment step uses this `vetUserId`
4. `POST /api/v1/owners` — create an owner under the tenant
5. `POST /api/v1/pets` — create a pet for that owner
6. `POST /api/v1/appointments` — schedule an appointment for the pet, assigned to the vet user
7. `PUT  /api/v1/appointments/{id}/confirm` — confirm the appointment
8. `POST /api/v1/vaccinations` — record a vaccination

Each request has test assertions and uses collection variables to chain ids
between steps. The tenant slug, email, scheduled date, and vaccination dates
are generated dynamically by the collection's pre-request scripts.

## Prerequisites

- The VetCare API is running and reachable. The collection defaults to
  `http://localhost:5000` and can be overridden with `--env-var baseUrl=...`.
- The PostgreSQL database used by the API is up and migrated.
- [Newman](https://www.npmjs.com/package/newman) is installed:

  ```bash
  npm install -g newman
  ```

## Running

From the repository root:

```bash
# default (localhost:5000)
newman run docs/e2e/vetcare.collection.json --env-var baseUrl=http://localhost:5000

# against an alternate host
newman run docs/e2e/vetcare.collection.json --env-var baseUrl=https://staging.vetcare.example.com
```

A successful run reports `Passed` for every test assertion and exits with code
`0`. Any non-2xx response or failed assertion exits with non-zero so the
collection can be wired into CI.

## What the collection verifies

- The auth flow returns a valid JWT and the admin caller can create a tenant-scoped
  `Vet` user via `POST /api/v1/users`; the captured `vetUserId` is then used in
  the appointment payload (the API rejects scheduling against non-`Vet` users).
- Domain invariants are respected at the API surface: appointments are scheduled
  with `status = 1` (Scheduled), then transition to `status = 2` (Confirmed) via
  the confirm endpoint.
- Vaccinations accept past `administeredAt` and a future `nextDueAt`, returning
  a `201 Created` with the new id.
