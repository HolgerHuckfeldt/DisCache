# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Branch note:** The active development branch is **`Subscribe`** (not `master`). `master` lags behind — it still has the old architecture where `CacheCore` was dead code and the in-memory accelerator was duplicated across controllers/hub. All guidance below describes `Subscribe`.

## Overview

DisC (DisCache) is a distributed cache server plus a .NET client library and an optional front-end proxy — a **"nearby-server"** cache built as a Redis alternative for environments where opening extra ports isn't possible. It started as a simple HTTP server, grew SignalR and gRPC transports, and has run in production for years. The server exposes the same cache (`Get`/`Set`/`Refresh`/`Remove`) over **REST, gRPC, and SignalR**, backed by a pluggable store, with server-side key-pattern subscriptions and a built-in statistics subsystem (**Stamon**). Auth is optional via the in-tree **EasyJwt**.

The project is being prepared for **open-source release** (Apache-2.0). See the OSS roadmap below.

## Build & Run

```powershell
dotnet build DisCache.sln
dotnet run --project DisCache        # cache server
dotnet run --project DisCProxy       # optional proxy/front-end
dotnet run --project WebApiDemo      # SDK demo (gRPC client)
```

There are **no test projects** yet (a test suite is a planned OSS phase). EF Core migrations auto-run on startup (`db.Database.Migrate()` in `Startup`). Runtime DB paths live in `appsettings.json` (now relative, e.g. `./data/disc.db`).

TFMs: `DisCache` and `DisCProxy` target **net8.0**; `DisCacheClient` is netstandard2.1, `DisCacheClient_2.0` netstandard2.0 (both link the shared `DisCache/Protos/DisCache.proto`). Note EF Core Sqlite is still pinned to 7.0.11 while everything else is 8/9 — alignment is a later cleanup.

## Architecture (Subscribe)

### CacheCore — the central wrapper (NOT an IDistributedCache decorator)
`DisCache/Services/CacheCore.cs` is a service that wraps the injected `IDistributedCache` (the real backend: Sqlite/Redis/DistributedMemory) plus `IMemoryCache` (the optional accelerator). It is registered as itself (`AddSingleton<CacheCore>()` in `Startup.cs`) and **injected as `CacheCore`** into `CacheController` and `CacheHub`, which delegate all operations to it. The accelerator logic (`TryGetFromMemoryCache`/`SetToMemoryCache`, gated by `Configuration.UseMemoryCacheAccelerator`) lives **only** in `CacheCore` — the old per-controller/hub duplication is gone.

### Events & server-side Subscribe (MediatR)
On `Set`/`Remove`, `CacheCore` publishes MediatR notifications (`KeyUpdatedNotification` / `KeyDeletedNotification` in `DisCache/Mediator/Notifications.cs`), gated by `DisCacheConfiguration.UseEvents`. Two handler pairs fan out:
- `KeyUpdateNotificationHandler` / `KeyDeleteNotificationHandler` → SignalR clients via `IHubContext<CacheHub>`, matching `CacheHub.KeyPatterns` and sending to SignalR groups.
- `KeyUpdateNotificationHandlerForGrpc` / `KeyDeleteNotificationHandlerForGrpc` → gRPC subscribers by writing to streams in `GprsCacheService.PatternSubscriptions`.

Clients subscribe to **prefix-glob patterns** (`pattern.EndsWith('*')` → prefix match, else exact):
- gRPC: `GprsCacheService.SubscribeToPattern` (bidirectional stream, `SubscriptionRequest{pattern, subscribe}` → `UpdateNotification{action, key}`).
- SignalR: `CacheHub.SubscribeToPattern` / `UnsubscribeFromPattern` using SignalR groups.

### Write-behind refresh
`DisCache/Services/QueuedHostedService.cs` + `IBackgroundTaskQueue` (max concurrency 4) run background work. Its only producer is `CacheCore.TryGetFromMemoryCache`, which queues a backend `RefreshAsync` on accelerator hits — gated by `Configuration.UseQueuedRefresh` (default `false`, currently **not bound from `appsettings.json`**).

### Transports & gRPC service
REST = `DisCache/Controllers/CacheController.cs`, SignalR = `DisCache/Hubs/CacheHub.cs` (mapped at `/CacheHub`), gRPC = `DisCache/gRpc/GprsCacheService.cs` (mapped via `MapGrpcService<GprsCacheService>()`). The old `DisCacheService.cs` was dead and has been removed. `KeyService` loads `KeyException` rows from `Disc.db` (EF) to `Ignore` keys matching StartsWith/EndsWith/Exact.

### Statistics — Stamon
`StamonService` (singleton) is injected into `CacheCore`, `CacheController`, `CacheHub` and called as `StatisticEvent("Request"|"Hit"|"Miss"|"Insert"|"Delete")`. Time-slot stats with slots declared in `Monitor:Provider` (`Type` = `Memory`/`EF`, `Scale`/`Duration`). Stamon is still **in production use** (feeds an external backend) — do not remove it; an OpenTelemetry path will be added *alongside* it in a later phase, and full removal only after the consuming backend is migrated.

### UI
There is currently **no admin UI** — the old `wwwroot/index.html` dashboard was removed. A Svelte 5 SPA is planned (will consume a future TS client). `WebApiDemo` (ASP.NET Core) and `FormsDemo` (WinForms) are **client SDK samples**, not admin UIs.

## Configuration (`appsettings.json`)

`DisC:UseRest` / `UsegRpc` / `UseSignalR` / `UseJwt` / `UseAuthorisation` / `Events` / `Swagger`; `CacheBackend:UseSqlite`/`UseInMemory`/`UseRedis` + `UseInMemoryAccelerator[Time]`; `Monitor` (stats DB + slot providers); `Redis`; `jwtTokenConfig` (EasyJwt). The server uses the legacy `Startup`/`IWebHostBuilder` pattern (not minimal hosting).

## Secrets

`appsettings.json` is committed with **placeholder values only**. Both `DisCache` and `DisCProxy` have a `<UserSecretsId>` in their csproj, so user secrets load automatically in `Development`, and environment variables override via `__` (e.g. `jwtTokenConfig__secret`). EasyJwt binds `jwtTokenConfig` via `GetSection`, so no EasyJwt code change was needed. Do not commit real secrets.

## OSS roadmap (in progress)

Open-sourcing under Apache-2.0. Completed phases:
- **Phase 1 — cleanup + blockers:** scrubbed secrets to placeholders + UserSecrets/env; untracked `*.db*`/`.idea`/backup `.csproj`/orphan `.snk` and extended `.gitignore`; deleted dead `Disc.Statistics`/`DiscPersistance.*` stacks, dead `DisCacheService.cs`, `Model/Statistic/Scale.cs`, and the leftover `IMemoryCache` injection in `CacheController`; restored `DisCache` TFM to net8.0; added `LICENSE`, root `README.md`, and a build CI workflow.

Planned: Stamon-Entschlackung + OpenTelemetry alongside (Phase 2), test suite + CI-with-tests (Phase 3), then a TS/JS client (REST + SignalR) and a Svelte 5 SPA UI, and a pluggable auth abstraction (EasyJwt as one option + corporate OIDC bearer).

## Conventions
- German comments appear in the cache code — match the surrounding comment language when editing.
- Serilog (console + file + Seq) is configured in `Program.cs`/`appsettings.json`.
- `SqliteCache` is a vendored fork of NeoSmart's MIT-licensed `NeoSmart.Caching.Sqlite` — keep its attribution (`SqliteCache/README.md`).