# DisC

DisC is a lightweight **distributed cache server** for .NET, designed as a *nearby-server* you can stand up next to your app without any of the infrastructure overhead of traditional caches.

> It was born out of necessity: in some environments opening the Redis port (or running a Redis service at all) is organizationally not possible. DisC gives you a persistent, multi-transport cache over ordinary HTTP ports instead — and has run in production for years, very efficiently for its scenarios.

## Highlights

- **Three transports, one cache** — REST controllers, gRPC, and a SignalR hub all expose the same `Get` / `Set` / `Refresh` / `Remove` operations. Enable/disable each via config.
- **Pluggable backends** — SQLite (default, no network port needed), Redis (when available), or in-memory. The server can also keep a fast in-memory accelerator tier in front of the backend.
- **Server-side subscribe** — clients subscribe to key patterns (`_test*`) and get `Update` notifications on writes/removes, fanned out via MediatR to SignalR groups and gRPC streams.
- **Optional JWT auth** — built-in bearer auth via the in-tree `EasyJwt` library, no external identity provider required.
- **Statistics** — built-in `Stamon` time-slot statistics (hit/miss/insert/delete) exposed via REST/gRPC/SignalR.

## Repository layout

| Project | Role |
|---|---|
| `DisCache` | The cache server (ASP.NET Core 8). REST + gRPC + SignalR. |
| `DisCacheClient` / `DisCacheClient_2.0` | .NET client libraries implementing `IDistributedCache` (REST/gRPC/SignalR). |
| `DisCProxy` | Optional front-end proxy / auth gateway (REST-only) in front of a remote DisC server. |
| `EasyJwt` | Optional standalone JWT auth. |
| `SqliteCache` | Vendored SQLite `IDistributedCache` (fork of NeoSmart.Caching.Sqlite, MIT). See [`SqliteCache/README.md`](SqliteCache/README.md). |
| `Stamon/*` | Statistics subsystem. |
| `WebApiDemo` / `FormsDemo` | Sample consumers of the client SDK. |

## Quickstart

```bash
dotnet build DisCache.sln
dotnet run --project DisCache
```

The server runs EF Core migrations on startup. Configure backends, transports and paths in [`DisCache/appsettings.json`](DisCache/appsettings.json).

## Configuration & secrets

`appsettings.json` is committed with **placeholder** values only. For local development, provide real values via **user secrets** or **environment variables** so they never live in the repo:

```bash
cd DisCache
dotnet user-secrets set "jwtTokenConfig:secret" "<your-long-random-secret>"
dotnet user-secrets set "jwtTokenConfig:User:demoUser" "<password>"
```

Environment variables use the `__` separator, e.g. `jwtTokenConfig__secret`. User secrets are loaded automatically in the `Development` environment.

## License

Apache License 2.0 — see [LICENSE](LICENSE). The vendored `SqliteCache` remains under its original MIT license (see [`SqliteCache/README.md`](SqliteCache/README.md)).