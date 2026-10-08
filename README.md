# diceroller-operative

The **Operative** microservice of the DiceRoller system. An authenticated user rolls two dice and queries their own
roll history with date filters, two-way sorting and pagination.

The service never calls `diceroller-useraccess`. It trusts only a JWT it validates itself (signature, issuer,
audience, lifetime); the caller's user id is the token's `sub` claim and is never read from the route, query or body.
Every query is scoped to that user.

## Run locally

Prerequisites: .NET 10 SDK, Docker.

### With Docker Compose (service + its own SQL Server)

```bash
cp .env.example .env      # then fill in NUGET_AUTH_TOKEN, MSSQL_SA_PASSWORD and JWT_SIGNING_KEY
docker compose up -d --build
```

The service listens on `http://localhost:5081` (`OPERATIVE_PORT`). Migrations are applied at startup in the
container. The compose file runs the service as `Development` (`ASPNETCORE_ENVIRONMENT`), so Scalar is at
`http://localhost:5081/scalar`. The database lives in the `operative-sqlserver-data` volume;
`docker compose down -v` removes it.

Building the image restores the `DiceRoller.BuildingBlocks.*` packages from GitHub Packages, so `NUGET_AUTH_TOKEN`
must be a GitHub personal access token with `read:packages`. It is passed to the build as a BuildKit secret and is not
stored in the image.

### With `dotnet run`

The packages come from the local feed `~/local-nuget` (see `nuget.config`). Start a SQL Server (for example
`docker compose up -d sqlserver`) and set the secrets once:

```bash
cd src/DiceRoller.Operative.Api
dotnet user-secrets set "ConnectionStrings:Operative" "Server=localhost,1433;Database=Operative;User Id=sa;Password=<sa password>;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:SigningKey" "<the key UserAccess signs with, at least 32 bytes>"
dotnet run
```

The service listens on `http://localhost:5102`. In Development, migrations run at startup, and OpenAPI
(`/openapi/v1.json`) and Scalar (`/scalar`) are available.

> The compose file doesn't publish SQL Server's port. To use it from `dotnet run`, add `ports: ["1433:1433"]` to
> the `sqlserver` service locally.

`src/DiceRoller.Operative.Api/DiceRoller.Operative.http` has a working example of every endpoint (it targets the
compose port 5081; change `@baseUrl` for `dotnet run`). Paste a token into `@token` first — see
[Getting a dev token](#getting-a-dev-token).

### Tests

```bash
dotnet build      # 0 warnings (TreatWarningsAsErrors)
dotnet test       # unit + integration; integration tests start SQL Server with Testcontainers, so Docker must run
```

The integration tests mint their own tokens with a test key; they don't need UserAccess.

## Configuration

Secrets come from user-secrets, environment variables or `.env`; they are never committed. Invalid options stop the
service at startup.

| Key (environment variable) | Default | Notes |
| --- | --- | --- |
| `ConnectionStrings:Operative` (`ConnectionStrings__Operative`) | — | Required. SQL Server connection string. |
| `Jwt:Issuer` (`Jwt__Issuer`) | `diceroller` | Expected `iss`. |
| `Jwt:Audience` (`Jwt__Audience`) | `diceroller` | Expected `aud`. |
| `Jwt:SigningKey` (`Jwt__SigningKey`) | — | Required secret, at least 32 bytes; must equal UserAccess's key. |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | — | Enables OTLP export of traces, metrics and logs. |

`.env.example` lists the variables Docker Compose uses (`OPERATIVE_PORT`, `ASPNETCORE_ENVIRONMENT`, the secrets
above and the SQL Server `sa` password).

## Endpoints

All `/api/v1/rolls` endpoints require `Authorization: Bearer <token>`.

| Route | Success |
| --- | --- |
| `POST /api/v1/rolls` (empty body) | `201` + `Location`, `DiceRollDto` |
| `GET /api/v1/rolls/{id}` | `200`, `DiceRollDto`; `404` if it doesn't exist **or** belongs to another user |
| `GET /api/v1/rolls` | `200`, `PagedResponse<DiceRollDto>` |
| `GET /health/live`, `GET /health/ready` (anonymous) | liveness; readiness includes the database |

`DiceRollDto` is `{ id, die1, die2, sum, rolledAtUtc }`. `PagedResponse<T>` is
`{ items, page, pageSize, totalCount, totalPages }`.

### Query parameters of `GET /api/v1/rolls`

| Parameter | Values | Notes |
| --- | --- | --- |
| `year` | 1–9999 | Rolls in that UTC year. |
| `month` | 1–12 | Requires `year`. |
| `day` | 1–31 | Requires `year` and `month`; the date must exist (no 30 February). |
| `sortBySum` | `asc` \| `desc` | Sort by `die1 + die2`. |
| `sortByDate` | `asc` \| `desc` | Sort by `rolledAtUtc`. |
| `page` | ≥ 1, default `1` | |
| `pageSize` | 1–100, default `10` | |

Date filters are half-open UTC ranges: `year=2026&month=10` means `2026-10-01T00:00Z ≤ rolledAtUtc < 2026-11-01T00:00Z`.

Ordering:

- `sortBySum` given → by sum first, then by date (`sortByDate`'s direction, default `desc`).
- only `sortByDate` → by date.
- neither → newest first.
- the roll id is always the final tiebreaker, so paging is stable.

### Errors

Every error is an RFC 9457 body (`application/problem+json`) with `status`, `title`, `detail`, `errorCode`,
`errors` (per-field messages for validation errors) and `traceId`:

| Status | `errorCode` |
| --- | --- |
| 400 | `Request.Invalid` with per-field `errors` (codes `RollsQuery.*`, e.g. `RollsQuery.DayRequiresYearAndMonth`, `RollsQuery.InvalidDate`) |
| 401 | `Auth.Unauthorized` (no token), `Auth.InvalidToken`, `Auth.TokenExpired` |
| 404 | `DiceRoll.NotFound` |
| 500 | `General.Unexpected` |

## Token contract

The service accepts compact JWTs signed with **HS256** using `Jwt:SigningKey`, as issued by `diceroller-useraccess`.

| Claim | Requirement |
| --- | --- |
| `iss` / `aud` | must equal `Jwt:Issuer` / `Jwt:Audience` |
| `sub` | the user id, a GUID — tokens without a GUID `sub` are rejected |
| `exp`, `nbf` | validated with 30 seconds of clock skew |

Only HS256 is accepted. Inbound claims are not remapped (`sub` stays `sub`). Other claims are ignored.

## Getting a dev token

**From UserAccess** (recommended — same issuer, audience and key): register a user with `POST /api/v1/users`
and exchange email + password at `POST /api/v1/tokens`; use `accessToken`. See the `.http` file in that repo.

**With `dotnet user-jwts`**, without UserAccess. By default `user-jwts` signs with a random key of its own, which
this service rejects (`Auth.InvalidToken`). Store the shared key (base64 of its UTF-8 bytes, `Length` = its byte
count) as the user-jwts key for issuer `diceroller` first, then create the token. `--name` becomes `sub`, so it must
be a GUID:

```bash
cd src/DiceRoller.Operative.Api
KEY='<the value of Jwt:SigningKey / JWT_SIGNING_KEY>'
dotnet user-secrets set "Authentication:Schemes:Bearer:SigningKeys:0:Id" "diceroller"
dotnet user-secrets set "Authentication:Schemes:Bearer:SigningKeys:0:Issuer" "diceroller"
dotnet user-secrets set "Authentication:Schemes:Bearer:SigningKeys:0:Value" "$(printf '%s' "$KEY" | base64 -w0)"
dotnet user-secrets set "Authentication:Schemes:Bearer:SigningKeys:0:Length" "$(printf '%s' "$KEY" | wc -c)"
dotnet user-jwts create --issuer diceroller --audience diceroller --name <a GUID> --output token
```

`user-jwts create` also adds an `Authentication:Schemes:Bearer` section to `appsettings.Development.json`. This
service doesn't read it; don't commit it.

## CI

`.github/workflows/ci.yml` restores (BuildingBlocks from GitHub Packages with the workflow's `GITHUB_TOKEN`), builds,
and runs unit and integration tests on every push and pull request. On `main` it also builds and pushes
`ghcr.io/<owner>/diceroller-operative`, tagged with the commit SHA and the version from `Directory.Build.props`.

The `DiceRoller.BuildingBlocks.*` packages must grant this repository read access
(package settings → *Manage Actions access*), otherwise the `GITHUB_TOKEN` can't restore them.
