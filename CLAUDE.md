# CLAUDE.md — diceroller-operative

## What this repo is

The **Operative** microservice of the DiceRoller system: an authenticated user rolls two dice and queries their own roll history.
It is one of four repos; it builds, tests and runs on its own and never references another repo's source code. It does not call UserAccess — it trusts only a validated JWT.

| Repo | Role |
| --- | --- |
| `diceroller-building-blocks` | Shared plumbing as NuGet packages (`DiceRoller.BuildingBlocks.*`) |
| `diceroller-useraccess` | Users + token issuing |
| `diceroller-operative` | **This repo** — dice rolls + history |
| `diceroller-platform` | YARP gateway, system docker-compose, end-to-end tests |

The current task is in `PLAN.md`. Do only the phase you are asked to do.

## Commands

```bash
dotnet build                                   # must finish with 0 warnings (TreatWarningsAsErrors)
dotnet test                                    # unit + integration; integration needs Docker running
docker compose up -d                           # SQL Server + this service
dotnet ef migrations add <Name> \
  -p src/DiceRoller.Operative.Infrastructure \
  -s src/DiceRoller.Operative.Api
dotnet user-jwts create --project src/DiceRoller.Operative.Api   # dev token for manual testing
```

## Endpoints (all require a Bearer token)

| Route | Notes |
| --- | --- |
| `POST /api/v1/rolls` | empty body → `201` + `Location` + `{ id, die1, die2, sum, rolledAtUtc }` |
| `GET /api/v1/rolls/{id}` | `404` if missing **or** owned by someone else |
| `GET /api/v1/rolls` | filters, sorting, paging below → `PagedResponse<RollDto>` |

## Rules specific to this service

- The user id comes **only** from the `sub` claim via `ICurrentUser`. Never accept a user id from the route, query or body. Every query filters by it.
- Token validation is real and strict: signature, issuer, audience, lifetime, `ValidAlgorithms = [HS256]`, `ClockSkew = 30s`, `MapInboundClaims = false`. Never relax these to make a test pass — fix the test's token instead.
- Dice: `IDiceRoller` backed by `RandomNumberGenerator.GetInt32(1, 7)`. `DieValue` throws `DomainException` outside 1–6.
- `Sum` is a persisted computed column. Indexes: `(UserId, RolledAtUtc)` and `(UserId, Sum, RolledAtUtc)`.
- Filters: `year`; `year`+`month`; `year`+`month`+`day`. `month` requires `year`, `day` requires both, and the date must exist (validator). Convert to a half-open UTC range `[start, end)` — never filter with `.Year` / `.Month` in LINQ.
- Sorting: `sortBySum` and `sortByDate`, each `asc|desc`. When both are given, **sum has more weight**: `ORDER BY Sum, RolledAtUtc, Id`. With no sort given, `RolledAtUtc desc`. `Id` is always the final tiebreaker.
- Paging: `page` ≥ 1 (default 1), `pageSize` 1–100 (default 10). Filter → sort → count → skip/take, in `RollsQueryBuilder` (pure LINQ, unit-tested).

<!-- ===== Everything below is identical in every DiceRoller repo ===== -->

## Architecture rules

- Projects: `Api → Application → Domain`; `Infrastructure → Application`. Domain references nothing except `DiceRoller.BuildingBlocks.Domain`.
- Domain: entities with private setters and factory methods (`DiceRoll.Create(...)`); invariants guarded inside the domain; value objects for concepts like `DieValue`.
- Application: one MediatR (12.x, Apache-2.0 — never 13+, which is commercially licensed) request + `IRequestHandler` per use case; controllers call `ISender.Send`. No pipeline behaviors — validation stays in `ValidationFilter`. Handlers depend on interfaces, never on EF or ASP.NET types.
- Api: thin controllers — bind → (validation filter runs) → call one handler → map `Result` to HTTP. No business logic, no `DbContext`.
- Infrastructure: EF Core and other external concerns — all behind interfaces declared in Application.
- Inject `TimeProvider` for time and interfaces for randomness. Never call `DateTime.UtcNow` or `Random` directly.

## Error handling — follow exactly

| Failure | How it is raised | Who turns it into HTTP |
| --- | --- | --- |
| Invalid input | FluentValidation validator | `ValidationFilter` → `Error.Validation` with per-field `Details` |
| Business rule (not found, bad query) | handler returns `Result.Failure(error)` | controller via `ToActionResult()` |
| Domain invariant broken | `throw new DomainException(DomainErrors.X)` | `GlobalExceptionHandler` → `Result.Failure(ex.Error)` |
| Anything unexpected | any other exception | `GlobalExceptionHandler` → `Error.Unexpected` (500) |

- Handlers return `Result` / `Result<T>`. They never `throw` for expected failures and never `catch` `DomainException`.
- Every failure goes through `ErrorMapper`, so every error body has the same RFC 9457 shape: `status`, `title`, `detail`, `errorCode`, `errors`, `traceId`. Never build a ProblemDetails or `BadRequest(...)` by hand. 401/403 bodies use the same shape.
- Error codes live in static classes per aggregate (`DiceRollErrors.NotFound`). Tests assert codes, not message text.

## Validation

- Every request DTO and query has a FluentValidation validator in the Application project, registered with `AddValidatorsFromAssemblyContaining<...>()`.
- No DataAnnotations on request types. Every rule sets `.WithErrorCode(...)` and `.WithMessage(...)`.
- Cross-field rules belong in the validator, not in controllers or handlers.

## API conventions

- Routes: plural nouns under `/api/v1`. Correct status codes: `201` + `Location` for creates, `400`, `401`, `403`, `404`, `409`.
- Every endpoint that returns a list takes `page` (≥ 1, default 1) and `pageSize` (1–100, default 10) and returns `PagedResponse<T>`, with a deterministic order (last sort key is `Id`).
- Endpoints are `async` and accept a `CancellationToken`; pass it all the way down.
- OpenAPI via `Microsoft.AspNetCore.OpenApi` + Scalar. Keep the `.http` file in the Api project up to date with a working example per endpoint.

## Data

- SQL Server, EF Core code-first. One database per service; never query another service's database.
- Configure entities with `IEntityTypeConfiguration<T>` classes; set max lengths, required fields and indexes explicitly.
- Reads use `AsNoTracking()` and project to DTOs. Migrations run at startup only in Development and in the container.

## Security and configuration

- Secrets (signing key, connection strings) come from user-secrets, environment variables or `.env`. Never commit them, never hardcode them, never log them.
- Options classes are validated with `ValidateOnStart()` so bad config fails at startup.
- The authorization fallback policy requires an authenticated user; anonymous endpoints (health, OpenAPI) opt out explicitly.

## Testing

- xUnit v3, Moq, Shouldly. Unit tests for validators, domain factories, the query builder and handlers; integration tests with `WebApplicationFactory` + Testcontainers (real SQL Server, never the EF in-memory provider).
- Name tests `Method_Scenario_ExpectedResult`. Every bug fix gets a test that fails without the fix.
- Integration tests mint their own tokens with the test key, issuer and audience. Cover: no token, expired, wrong signature, wrong issuer, wrong audience → `401`.

## Code style

- .NET 10, C# latest, nullable enabled, file-scoped namespaces, primary constructors where they read well, `sealed` by default.
- Central Package Management: versions only in `Directory.Packages.props`.
- No commented-out code, no `TODO` without a matching PLAN.md item.

## How to work

1. Read `PLAN.md` and restate the phase's tasks before writing code. In plan mode, propose the design and wait for approval.
2. Work in small steps (domain → application → infrastructure → api → tests). After each step run `dotnet build` and `dotnet test`; fix failures before moving on.
3. Tick the matching checkbox in `PLAN.md` when a task is done.
4. Do not add features, endpoints or NuGet packages that `PLAN.md` doesn't list — ask first.
5. Do not change anything in another repo. If BuildingBlocks needs a change, stop and describe it.
6. Do not commit or push unless asked. When asked, use Conventional Commits (`feat:`, `fix:`, `test:`, `chore:`).
7. Finish by checking the phase's "Done when" list item by item and reporting anything not met.
