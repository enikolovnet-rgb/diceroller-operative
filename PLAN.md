# PLAN.md — diceroller-operative (Phase 2)

## Goal

An authenticated user rolls two dice and queries their own history with filters, two-way sorting and pagination. Every endpoint requires a valid JWT, validated for real.

Depends on `DiceRoller.BuildingBlocks.*` v0.1.0. No dependency on `diceroller-useraccess` at build, test or run time — tests mint their own tokens.

## 1. Repo setup (service template)

- [x] `DiceRoller.Operative.slnx`, `global.json`, `Directory.Build.props`, `Directory.Packages.props` (BuildingBlocks pinned to `0.1.0`), `nuget.config`, `.editorconfig`, `.gitignore`
- [x] Projects:
  - `src/DiceRoller.Operative.Domain` → BuildingBlocks.Domain
  - `src/DiceRoller.Operative.Application` → Domain, BuildingBlocks.Contracts, FluentValidation, MediatR 12.5.0
  - `src/DiceRoller.Operative.Infrastructure` → Application, EF Core SqlServer
  - `src/DiceRoller.Operative.Api` → Application, Infrastructure, BuildingBlocks.Web
  - `tests/DiceRoller.Operative.UnitTests`
  - `tests/DiceRoller.Operative.IntegrationTests`
- [x] Builds green with empty projects before any feature work

## 2. Domain

- [x] `DieValue` value object: 1–6, throws `DomainException(DiceRollErrors.InvalidDieValue)` otherwise
- [x] `DiceRoll` aggregate: `Id` (Guid v7), `UserId` (Guid, no FK — another service's data), `Die1`, `Die2`, `Sum` (derived), `RolledAtUtc`
- [x] `DiceRoll.Create(Guid userId, IDiceRoller roller, TimeProvider time)`; empty `userId` throws `DomainException`
- [x] `IDiceRoller` interface (in Domain)
- [x] `DiceRollErrors`: `NotFound`, `InvalidDieValue`, `InvalidUser`

## 3. Application

- [x] Interfaces: `IDiceRollRepository`, `ICurrentUser`
- [x] `RollDiceCommand` / `RollDiceHandler` → `Result<DiceRollDto>`
- [x] `GetRollQuery` / `GetRollHandler` → `Result<DiceRollDto>`; `NotFound` when missing **or** owned by another user
- [x] `RollsQuery` record: `Year?`, `Month?`, `Day?`, `SortBySum?`, `SortByDate?` (`SortDirection` enum `Asc`/`Desc`), `Page`, `PageSize`
- [x] `RollsQueryValidator`:
  - `Year` 1–9999; `Month` 1–12 and requires `Year`; `Day` 1–31 and requires `Year` + `Month`
  - `Year/Month/Day` must form a real date (no 30 February)
  - `Page` ≥ 1; `PageSize` 1–100 (reuse `PagedQueryValidator` rules)
  - every rule has `.WithErrorCode(...)` and `.WithMessage(...)`
- [x] `RollsQueryBuilder` (pure LINQ over `IQueryable<DiceRoll>`):
  - filter: convert year / year-month / year-month-day into a half-open UTC range `[start, end)`; filter `RolledAtUtc >= start && RolledAtUtc < end`
  - sort: `SortBySum` present → order by `Sum` first, then `RolledAtUtc` (direction from `SortByDate`, default desc); only `SortByDate` → by `RolledAtUtc`; neither → `RolledAtUtc desc`; always `ThenBy(Id)` last
  - page: count total, then skip / take
- [x] `RollsQuery` / `GetRollsHandler` → `Result<PagedResponse<DiceRollDto>>`, always scoped to `ICurrentUser.UserId`
- [x] `DiceRollDto { Id, Die1, Die2, Sum, RolledAtUtc }`
- [x] `AddApplication()` registration extension

## 4. Infrastructure

- [x] `OperativeDbContext`, `DiceRollConfiguration`:
  - table `DiceRolls`; `Die1`, `Die2` as `tinyint` with check constraints 1–6
  - `Sum` as persisted computed column `Die1 + Die2`
  - indexes `(UserId, RolledAtUtc)` and `(UserId, Sum, RolledAtUtc)`
- [x] Initial migration `InitialCreate`
- [x] `DiceRollRepository` (queries use `AsNoTracking()` and project to DTOs)
- [x] `CryptoDiceRoller : IDiceRoller` using `RandomNumberGenerator.GetInt32(1, 7)`
- [x] `AddInfrastructure(IConfiguration)`; DB health check

## 5. Api

- [x] `Program.cs`: `AddServiceDefaults()`, `AddApplication()`, `AddInfrastructure()`, `AddJwtAuthentication()`; fallback policy = authenticated; migrations at startup in Development and in the container
- [x] `ICurrentUser` implementation: reads `sub`, parses Guid; never takes a user id from the request
- [x] `RollsController` (all endpoints require auth):
  - `POST /api/v1/rolls` → `201` + `Location`
  - `GET /api/v1/rolls/{id:guid}` → `200` / `404`
  - `GET /api/v1/rolls?year=&month=&day=&sortBySum=&sortByDate=&page=&pageSize=` → `200 PagedResponse<DiceRollDto>`
- [x] `/health/*` and OpenAPI explicitly `[AllowAnonymous]`
- [x] `appsettings.json` with JWT issuer/audience matching the token contract; key via user-secrets / env
- [x] `DiceRoller.Operative.http` with roll, get-by-id and several list queries (filters, both sorts, page 2)

## 6. Tests

- [x] Unit: `DieValue` (0 and 7 throw), `DiceRoll.Create` with a fake roller and `FakeTimeProvider`
- [x] Unit: `RollsQueryValidator` — month without year, day without month, 30 Feb, page 0, pageSize 101
- [x] Unit: `RollsQueryBuilder` over an in-memory list:
  - year / month / day ranges include boundaries correctly (start inclusive, end exclusive)
  - both sorts: sum dominates, date breaks ties; each direction combination
  - default order; stable paging with equal sums and timestamps
  - `TotalCount` / `TotalPages`
- [x] Integration (Testcontainers SQL Server + `WebApplicationFactory`, tokens minted with the test key):
  - no token / expired / wrong signature / wrong issuer / wrong audience → 401, standard body
  - roll → 201, dice 1–6, sum correct
  - list returns only the caller's rolls (two users)
  - filter + sort + page end to end against SQL Server
  - another user's roll by id → 404
  - invalid query → 400 with per-field `errors`

## 7. Container and CI

- [ ] Multi-stage `Dockerfile`, non-root user, `HEALTHCHECK` on `/health/live`
- [ ] `docker-compose.yml`: this service + its own SQL Server 2022 (healthcheck, named volume); settings from `.env`
- [ ] `.env.example`; `.env` git-ignored
- [ ] GitHub Actions `ci.yml`: restore → build → unit + integration tests → on `main`, push image `ghcr.io/<owner>/diceroller-operative` tagged with SHA and version
- [ ] `README.md`: purpose, run locally, configuration, endpoints and query parameters, the token contract it expects, how to get a dev token (`dotnet user-jwts`)

## Done when

- [ ] `dotnet build` with 0 warnings, `dotnet test` green
- [ ] `docker compose up` from a fresh clone of this repo alone works, and every request in the `.http` file succeeds with a dev token
- [x] Every error response (400, 401, 404, 500) has the standard body with `errorCode` and `traceId`
