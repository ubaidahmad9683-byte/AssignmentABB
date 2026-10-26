# Three in a Row — Tic Tac Toe

## 1. Project overview

Three in a Row is a browser-based Tic Tac Toe application built with Angular and a .NET 8 REST API. It supports two-player and computer-opponent games, move history and undo, win/draw detection, and a persistent scoreboard. SQL Server is the durable source of truth; optional caching, JWT authentication for scoreboard administration, and health checks are included.

## 2. Tech stack

- **Frontend:** Angular, TypeScript, RxJS
- **Backend:** ASP.NET Core Web API on .NET 8, REST, Swagger/OpenAPI
- **Data:** Entity Framework Core and SQL Server; migrations run at API startup
- **Caching:** `IMemoryCache` plus `IDistributedCache`; Redis can be enabled for distributed deployments
- **Security/operations:** JWT bearer authentication, role authorization, Problem Details, structured logging, health checks, optional OpenTelemetry metrics
- **Tests:** xUnit backend tests and Angular/Vitest frontend tests

## Solution layout

```text
backend/
  TicTacToe.Domain/
    Enums/                   GameMode, GameStatus, Player
    Classes/                 GameRules
    Models/                  Immutable game domain models
  TicTacToe.Application/
    Interfaces/              Business, repository, cache, and auth contracts
    Classes/                 Business services
    Models/                  Application paging/query/user models
  TicTacToe.Infrastructure/
    DataAccess/              DbContext, entities, configurations, migrations
    Repository/Classes/      EF repositories and generic repository
    Caching/Classes/         Memory + distributed cache-aside implementation
    DependencyInjection/     Infrastructure service registration
  TicTacToe.Api/
    Controllers/             Auth, games, scoreboard
    Models/                  Validated request and response DTOs
    Middleware/              Problem Details and request metrics
    Authentication/Classes/ JWT issuer
  TicTacToe.Tests/           xUnit unit, integration, repository, and cache tests
frontend/                    Angular app and API client
TicTacToe.slnx
```

Dependencies point inward: API and Infrastructure depend on Application/Domain; Domain has no infrastructure dependency. EF Core entities are not API response types. `IRepository<TEntity>` is a small shared EF repository for basic persistence; game-specific transitions and transactional scoreboard updates stay in `GameRepository`.

## 3. Features implemented

- Responsive 3 × 3 browser game with Two Player and Play Computer modes.
- Backend-validated turns and moves, win/draw detection, and winning-cell highlighting.
- Move history, mode-specific undo, and new-game reset.
- SQL-backed session scoreboard, updated once when a game completes; separate Admin-protected reset.
- Basic computer strategy: win, block, center, corner, then any available cell.
- Cache-aside services, optional Redis, JWT/Admin authorization, Swagger, liveness/readiness checks, and request metrics.

### NuGet packages

| Area | NuGet package(s) |
| --- | --- |
| SQL Server / EF Core | `Microsoft.EntityFrameworkCore.SqlServer` 8.0.33 |
| JWT | `Microsoft.AspNetCore.Authentication.JwtBearer` 8.0.33 |
| Redis distributed cache | `Microsoft.Extensions.Caching.StackExchangeRedis` 8.0.33 |
| Health checks | `AspNetCore.HealthChecks.SqlServer` 8.0.2, `AspNetCore.HealthChecks.Redis` 8.0.1 |
| OpenAPI | `Microsoft.AspNetCore.OpenApi` 8.0.33, `Swashbuckle.AspNetCore` 6.6.2 |
| Metrics (opt-in) | OpenTelemetry hosting, OTLP exporter, ASP.NET Core and runtime instrumentation 1.20.1 |
| Tests | xUnit and `Microsoft.AspNetCore.Mvc.Testing` |

### Requirements

- .NET 8 SDK and runtime
- Node.js 22+ and npm
- SQL Server reachable from the API (the configured local host is `DESKTOP-8T86VC6`)
- Optional: Redis for shared distributed caching. Local development defaults to in-process distributed memory cache.

## 4. Run the backend locally

### Configure secrets

From the repository root, initialize .NET User Secrets and set a unique signing key and first administrator credentials. Replace the sample values with your own:

```powershell
dotnet user-secrets init --project backend\TicTacToe.Api\TicTacToe.Api.csproj
dotnet user-secrets set "Authentication:Jwt:SigningKey" "replace-with-a-unique-random-secret-of-at-least-32-bytes" --project backend\TicTacToe.Api\TicTacToe.Api.csproj
dotnet user-secrets set "Authentication:BootstrapAdmin:Username" "admin" --project backend\TicTacToe.Api\TicTacToe.Api.csproj
dotnet user-secrets set "Authentication:BootstrapAdmin:Password" "replace-with-a-strong-password" --project backend\TicTacToe.Api\TicTacToe.Api.csproj
```

The signing key must contain at least 32 UTF-8 bytes. Never commit secrets. The initial administrator is created once, using ASP.NET Core's password hasher; subsequent starts do not overwrite its password.

`backend\TicTacToe.Api\appsettings.json` uses Windows Integrated Authentication against the local `SQLEXPRESS` instance on `DESKTOP-8T86VC6`, database `TicTacToeApi`. EF migrations create and update this database at startup. This avoids the separate default SQL Server instance, whose existing `TicTacToe` database is in `RECOVERY_PENDING`. Update `ConnectionStrings:GameDatabase` if your SQL instance or authentication method differs. Ensure that the configured Windows identity can create/use that database.

### Optional Redis

For a local Redis instance, for example with Docker Desktop:

```powershell
docker run --name tictactoe-redis -p 6379:6379 -d redis:7-alpine
dotnet user-secrets set "Cache:UseRedis" "true" --project backend\TicTacToe.Api\TicTacToe.Api.csproj
dotnet user-secrets set "Cache:RedisConnection" "localhost:6379,abortConnect=false" --project backend\TicTacToe.Api\TicTacToe.Api.csproj
```

Redis is optional for a single local instance. With Redis enabled, use a protected Redis deployment with TLS and authentication in production; configure its connection string from a secret manager. The cache is cache-aside: database reads fill cache, writes invalidate cache. An in-memory layer is valid for 10 seconds; distributed keys have a two-minute lifetime and version tokens invalidate other instances. If Redis is down, the API logs cache degradation and reads from SQL Server; cache errors do not hide database errors.

### Start the API

```powershell
dotnet restore TicTacToe.slnx
dotnet run --project backend\TicTacToe.Api\TicTacToe.Api.csproj --urls http://localhost:5089
```

On startup, EF applies SQL migrations and bootstraps the first Admin if configured. OpenAPI/Swagger UI is available at `http://localhost:5089/swagger` in Development.

## 5. Run the frontend locally

In another terminal from the repository root:

```powershell
cd frontend
npm install
npm start
```

Open `http://localhost:4200`. The development proxy forwards `/api` to port 5089. Use the configured bootstrap Admin credentials only when choosing **Reset scoreboard**; gameplay and read endpoints remain available for the original browser-game flow. The access token is held in `sessionStorage`, attached by the Angular interceptor, and can be cleared with Sign out.

## 6. API endpoint summary

All endpoints use camelCase JSON and string enum names. Game rows and columns use zero-based values `0`–`2`; the UI displays positions starting at one. Errors use Problem Details for API exceptions.

| Method | Route | Behavior |
| --- | --- | --- |
| `POST` | `/api/auth/token` | Validate username/password; returns short-lived JWT. Login is limited to 10 requests/minute/IP. |
| `POST` | `/api/games` | Create a game; body `{"mode":"TwoPlayer"}` or `{"mode":"Computer"}`; returns `201 Created` and Location. |
| `GET` | `/api/games/{id}` | Get state. |
| `GET` | `/api/games?page=1&pageSize=20` | Paginated sessions; page is capped at 1,000,000 and page size at 100. |
| `POST` | `/api/games/{id}/moves` | Submit `{ "player":"X", "row":0, "column":0 }`; computer replies are included in the state. |
| `POST` | `/api/games/{id}/undo` | Undo one move in two-player mode or the last human/computer pair in computer mode. |
| `POST` | `/api/games/{id}/reset` | Reset the current game while retaining mode and ID. |
| `GET` | `/api/scoreboard` | Read X wins, O wins, and draws. |
| `POST` | `/api/scoreboard/reset` | Requires a valid JWT with the `Admin` role. |
| `GET` | `/health/live` | Process liveness, no dependency probes. |
| `GET` | `/health/ready` | SQL Server readiness and Redis readiness when enabled. |

Game state includes board, player/mode/status, winner, winning cell indices, move history, and a scoreboard snapshot. SQL stores game state JSON with SQL Server row-version concurrency protection; scoreboard updates are atomic and persisted in the same transaction as game completion. Read-only EF Core queries use `AsNoTracking()`. EF Core parameters are used for database access; no interpolated SQL is built.

## 7. How to run tests

```powershell
dotnet build TicTacToe.slnx
dotnet test backend\TicTacToe.Tests\TicTacToe.Tests.csproj
```

Tests cover game rules, invalid moves, win/draw handling, computer priority, both undo modes, cache hits and cross-instance invalidation, health, authentication-token response, and role authorization. Frontend checks:

```powershell
cd frontend
npm test
npm run build
```

The API records structured request logs and a `System.Diagnostics.Metrics` request-duration histogram (`TicTacToe.Api`, `http.server.request.duration`). To export request/runtime metrics to an OpenTelemetry Collector, set `Monitoring:EnableOtlp=true` and configure `OTEL_EXPORTER_OTLP_ENDPOINT` (and any required headers) via deployment environment. Export is opt-in. EF SQL command logging defaults to Warning to avoid high-volume query logs.

## 8. AI tools and prompt summary

AI assistance was provided with **GitHub Copilot in VS Code**. The prompts supplied for this project asked for:

- A browser Tic Tac Toe application implementing the detailed board, turns, win/draw, history, undo, scoreboard, and computer-player requirements.
- A .NET 8 REST API with layered/Clean Architecture, SQL Server and EF Core, reusable repositories/services, caching, JWT and role authorization, centralized error handling, health checks, and tests.
- Setup and review documentation, including run instructions, API endpoints, tests, design choices, assumptions, and limitations.

AI assistance was used to draft and refine implementation and documentation. The generated changes were reviewed and tested against the specified gameplay rules, API behavior, database integration, and local run workflow.

## 9. Design decisions

- **Backend owns game rules and state.** The Angular client renders API responses; move validation, game completion, history, and scoreboard updates are performed by the backend.
- **Layered architecture.** Controllers handle HTTP concerns, application services coordinate rules and use cases, repositories encapsulate persistence, and Domain contains game rules and domain models.
- **SQL Server is the source of truth.** EF migrations run at startup. The local configuration targets `DESKTOP-8T86VC6\SQLEXPRESS` and the separate `TicTacToeApi` database.
- **Atomic result tracking.** Game state and its one-time scoreboard result are persisted in a transaction. Undo is disabled after completion so the recorded result remains final.
- **Cache-aside is an optimization.** Cache entries are invalidated after writes; SQL errors are not hidden by success-shaped cache fallbacks. Redis is optional for local single-instance use.
- **Admin-only operation.** Scoreboard reset requires a JWT with the Admin role; gameplay and read operations remain anonymous for this local exercise.

## 10. Clarifications and assumptions

- **Undo/result policy:** Option A is used—undo is unavailable after a win or draw, so the scoreboard is not reversed.
- **Computer mode:** Human is X and computer is O. The backend chooses and records the computer move as part of the move operation.
- **Database:** The default `MSSQLSERVER` instance has a separate `TicTacToe` database reported as `RECOVERY_PENDING`. This sample does not modify it; it uses `SQLEXPRESS` and `TicTacToeApi`. Change the connection string if your intended instance/database differs.
- **Legacy migration:** The previously applied Product migration remains in migration history so existing databases can continue upgrading safely. Product endpoints and application code have been removed; any existing `Products` table/data is left untouched and is not exposed by the API.
- **Bootstrap account:** An Admin is created only when both bootstrap username and password secrets are set. The password is hashed; it is not stored in application settings.
- **Local services:** Redis is optional and in-process distributed memory is used unless Redis is enabled. For multiple API instances, configure shared Redis.

## 11. Known limitations

- Gameplay routes are anonymous and games have no user ownership model; add identity/ownership policies before public deployment.
- The sample Admin bootstrap is one-time; it does not provide password reset, lockout, audit, or managed user provisioning.
- Game state is serialized as JSON in SQL Server. Game pagination is bounded but does not filter by owner or status.
- In-memory cache entries are process-local. If Redis is unavailable during invalidation, another node may hold stale values until the distributed cache TTL expires.
- The sample local SQL configuration uses Windows Integrated Authentication and is intended for development. Production deployments need managed secrets, appropriate database permissions, HTTPS, and secured SQL/Redis connections.
- No GitHub remote was provided; this README and implementation are in the local repository.

## 12. Future improvements

- Add user accounts, game ownership, multiplayer session access controls, and an account lifecycle.
- Add broader API integration tests for all game transitions and run performance/load tests against SQL Server and Redis.
- Add structured dashboards/traces and production-ready deployment manifests and secret-management guidance.
- Add scoreboard/game-history filtering and retention policies.
- Configure CI to build and test the .NET solution and Angular application on each change.
