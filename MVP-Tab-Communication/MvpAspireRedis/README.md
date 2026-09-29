# MvpAspireRedis — server-sync board with Redis

Extends the **MvpServerSync** cross-window drag demo with [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/) and **Redis** so multiple web replicas share:

| Concern | In `MvpServerSync` | Here |
| --- | --- | --- |
| Board items | In-memory `BoardStore` singleton | `RedisBoardStore` (`mvp:board:items`) |
| Drag events | In-memory `DragSessionHub` | `RedisDragSessionHub` (pub/sub `mvp:drag:events`) |
| Hosting | Single `dotnet run` | AppHost + **2 web replicas** + Redis container |

No JavaScript / BroadcastChannel — same Blazor Server + coordinator pattern as the original board MVP.

## Prerequisites

- .NET 10 SDK
- Docker (Aspire runs Redis in a container)
- AppHost SDK aligned with Aspire packages: `Aspire.AppHost.Sdk/13.5.4` in `AppHost/MvpAspireRedis.AppHost.csproj` (must match `Aspire.Hosting.*` package versions). If you see *“Newer version of the Aspire.Hosting.AppHost package is required”*, bump the SDK and optionally `dotnet tool update -g aspire.cli`.

## Run

```bash
cd MvpAspireRedis
dotnet run --project AppHost
```

1. Open the **Aspire dashboard** link from the console.
2. Open fixed URLs (proxyless ports from AppHost):
   - **Replica A:** http://127.0.0.1:5280/board  
   - **Replica B:** http://127.0.0.1:5281/board  
   (Dashboard links for **web-a** / **web-b** should match those ports after restart.)
3. Drag boxes between the two windows.
4. The header should show **Replica A** vs **Replica B** (via `MVP_DEMO_REPLICA`). **node** is a per-process id (new on each restart).

Optional: `GET /api/instance` returns `{ shortId, replicaIndex }`.

### Why not one “web” link with `WithReplicas(2)`?

Aspire fronts replicas with a **single proxied URL** that load-balances. That is good for realism but bad for forcing “window 1 → replica 0, window 2 → replica 1”. This sample uses **web-a** / **web-b** instead. To try random load balancing, see the comment in `AppHost/AppHost.cs`.

## Project layout

| Path | Role |
| --- | --- |
| `AppHost/AppHost.cs` | Redis + **web-a** / **web-b** (two URLs, same web project) |
| `Web/Services/Board/RedisBoardStore.cs` | Authoritative board JSON in Redis + change notifications |
| `Web/Services/DragSession/RedisDragSessionHub.cs` | Drag pub/sub + session keys in Redis |
| `Web/Services/DragDrop/DragDropCoordinator.cs` | Same per-circuit logic as `MvpServerSync` |
| `Web/Components/Pages/Board.razor` | Kanban UI |

## Plug Redis into your app

1. Add `Aspire.Hosting.Redis` to AppHost and `Aspire.StackExchange.Redis` to the web project.
2. `builder.AddRedis("redis")` in AppHost; `WithReference(redis)` on the web project.
3. In web `Program.cs`: `builder.AddRedisClient("redis")`.
4. Replace `IBoardStore` / `IDragSessionHub` registrations with the Redis implementations (or your own types using the same keys/channels in `Infrastructure/RedisKeys.cs`).

For production, keep authoritative data in Redis or your database; treat drag sessions as short-lived keys with TTL (this sample uses 2 minutes).

See [IMPLEMENTATION.md](./IMPLEMENTATION.md) for architecture, sequences, Redis keys, and production notes. Shared protocol index: [../IMPLEMENTATION.md](../IMPLEMENTATION.md).
