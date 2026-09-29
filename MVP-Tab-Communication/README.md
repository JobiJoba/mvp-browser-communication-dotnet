# Blazor Server Cross-Tab Communication — MVPs

Demos of cross-window drag-and-drop for Blazor Server (.NET 10).

## Projects

| Project | What it demos | Transport | Port | Deep dive |
| --- | --- | --- | --- | --- |
| [`MvpBroadcastChannel/`](MvpBroadcastChannel/) | Shared board boxes | `BroadcastChannel` + JSInterop | http://localhost:5201 | [IMPLEMENTATION](MvpBroadcastChannel/IMPLEMENTATION.md) |
| [`MvpServerSync/`](MvpServerSync/) | Shared board boxes | In-process hub (no JSInterop) | http://localhost:5202 | [IMPLEMENTATION](MvpServerSync/IMPLEMENTATION.md) |
| [`MvpAspireRedis/`](MvpAspireRedis/) | Same board as Server Sync | Redis + **web-a** / **web-b** (Aspire AppHost) | Dashboard → **web-a** & **web-b** (:5280 / :5281) | [IMPLEMENTATION](MvpAspireRedis/IMPLEMENTATION.md) |
| [`MvpAspirePostgres/`](MvpAspirePostgres/) | Same board via **Api** + frontend | Postgres API + SignalR; **web-a** / **web-b** | Dashboard → **web-a** & **web-b** (:5290 / :5291), Api :5295 | [IMPLEMENTATION](MvpAspirePostgres/IMPLEMENTATION.md) |
| [`MvpAspireMessages/`](MvpAspireMessages/) | Messages list → detail → change state | REST Api + Blazor (Aspire) | http://127.0.0.1:5300/messages, Api :5305 | [IMPLEMENTATION](MvpAspireMessages/IMPLEMENTATION.md) |
| [`MvpCaseSidePanelServer/`](MvpCaseSidePanelServer/) | Per-tab managed-case side panel | In-process hub (no JSInterop) | http://localhost:5203 | [IMPLEMENTATION](MvpCaseSidePanelServer/IMPLEMENTATION.md) |
| [`MvpCaseSidePanelBroadcast/`](MvpCaseSidePanelBroadcast/) | Per-tab managed-case side panel | `BroadcastChannel` + JSInterop | http://localhost:5204 | [IMPLEMENTATION](MvpCaseSidePanelBroadcast/IMPLEMENTATION.md) |

## Important demo constraint

Use **two side-by-side browser windows**, not two tabs in one window. Switching tabs usually cancels an HTML5 drag.

## Case side panel (practical use case)

Each window opens the **same workspace page** with an empty side panel. You choose which demo dataset to load from the left menu:

- **Generate cases for Tab A** → Mariana-related links  
- **Generate cases for Tab B** → Jean-related links  

Clicking a link shows case detail in that window. Dragging a link into the other window’s panel **moves** it.

```bash
dotnet run --project MvpCaseSidePanelServer
# or
dotnet run --project MvpCaseSidePanelBroadcast
```

1. Open `/` in two windows side by side.
2. Left window: click **Generate cases for Tab A**.
3. Right window: click **Generate cases for Tab B**.
4. Drag a link from one “Managed cases” panel onto the other; confirm the move.

## Shared board demos

```bash
dotnet run --project MvpBroadcastChannel
# or
dotnet run --project MvpServerSync
```

Open `/board` in two windows. Boxes are shared by id: moving Box A updates both windows.

### Multi-container / scale-out

[`MvpAspireRedis/`](MvpAspireRedis/) runs the same server-sync board with Redis-backed `IBoardStore` and `IDragSessionHub`, plus two replicas — use it to verify cross-window sync when each Blazor circuit may hit a different container. Requires Docker. See [MvpAspireRedis/README.md](MvpAspireRedis/README.md).

[`MvpAspirePostgres/`](MvpAspirePostgres/) is the multi-replica board with a **separate Api** that owns PostgreSQL (REST + SignalR). Blazor **web-a** / **web-b** only call that API — the pattern when a board team ships a backend service. Requires Docker. See [MvpAspirePostgres/README.md](MvpAspirePostgres/README.md). SignalR incremental cost (EUR): [MvpAspirePostgres/AZURE-COST-ESTIMATE.md](MvpAspirePostgres/AZURE-COST-ESTIMATE.md).

## Messages list / detail

[`MvpAspireMessages/`](MvpAspireMessages/) is an Aspire shell (Api + Blazor) for inbox-style navigation: overview → detail → change state → back. No Docker. See [MvpAspireMessages/README.md](MvpAspireMessages/README.md).

```bash
dotnet run --project MvpAspireMessages/AppHost
```

Open http://127.0.0.1:5300/messages

## When to pick which transport

**Prefer server hub when:**

- You want pure C# / Blazor Server patterns.
- You may later sync across users or machines (swap the singleton for Redis pub/sub, Postgres `NOTIFY`, or SignalR).
- You want a server-side audit point for transfers.

**Prefer BroadcastChannel when:**

- Same-browser only is enough.
- You want minimal latency and no server hop for drag messages.

## Docs

- **Index** (protocol overview + Azure decision tree): [IMPLEMENTATION.md](IMPLEMENTATION.md)
- **Per MVP** (mermaid flows, DI, file map): see the Deep dive column above.
