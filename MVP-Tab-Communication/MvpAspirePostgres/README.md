# MvpAspirePostgres — frontend + Postgres API backend

Same cross-window drag board as the other Aspire MVPs, split like a real team boundary:

| Layer | Project | Role |
| --- | --- | --- |
| **Backend** | `Api/` | Owns PostgreSQL (`board_items`, `drag_sessions`), REST + SignalR |
| **Frontend** | `Web/` | Blazor Server UI; HTTP for commands, SignalR for fan-out |
| **Hosting** | `AppHost/` | Postgres + **api** + **web-a** + **web-b** |

```text
web-a ──HTTP + SignalR──┐
                        ├──► api ──► PostgreSQL
web-b ──HTTP + SignalR──┘
```

No JavaScript BroadcastChannel. Same `DragDropCoordinator` as `MvpServerSync`; only `IBoardStore` / `IDragSessionHub` talk to the API.

## Prerequisites

- .NET 10 SDK
- Docker (Aspire runs Postgres)
- Aspire packages `13.5.4` (see AppHost csproj)

## Run

```bash
cd MvpAspirePostgres
dotnet run --project AppHost
```

1. Open the Aspire dashboard.
2. Fixed URLs:
   - **Replica A:** http://127.0.0.1:5290/board  
   - **Replica B:** http://127.0.0.1:5291/board  
   - **Api board JSON:** http://127.0.0.1:5295/api/board  
3. Drag boxes between the two windows.

## API surface

| Method | Path | Purpose | Status |
| --- | --- | --- | --- |
| `GET` | `/api/board` | List board items | `200` |
| `POST` | `/api/board/move` | `{ itemId, zoneId }` | `200` / `400` / `404` / `409` |
| `POST` | `/api/drag/begin` | Start drag session | `202` / `400` |
| `POST` | `/api/drag/cancel` | Cancel drag | `202` / `400` / `404` |
| `POST` | `/api/drag/accept` | Accept drop | `202` / `400` / `404` |
| SignalR | `/hubs/board` | Server → client: `BoardChanged`, `DragEvent` | — |

Web-only (not on Api): `GET /api/instance` returns the replica label for the debug header.

## Project layout

| Path | Role |
| --- | --- |
| `Api/Services/BoardService.cs` | Postgres board rows + SignalR `BoardChanged` |
| `Api/Services/DragSessionService.cs` | Drag session rows + SignalR `DragEvent` |
| `Api/Services/DragSessionSweeper.cs` | TTL cleanup; broadcasts `Cancelled` for expired sessions |
| `Api/Hubs/BoardRealtimeHub.cs` | Push hub (clients receive only) |
| `Web/Services/Api/BoardApiClient.cs` | Typed HTTP client (`https+http://api`) |
| `Web/Services/Api/ApiRealtimeConnection.cs` | SignalR client per web replica |
| `Web/Services/Board/ApiBoardStore.cs` | `IBoardStore` over HTTP + SignalR |
| `Web/Services/DragSession/ApiDragSessionHub.cs` | `IDragSessionHub` over HTTP + SignalR |

## Why SignalR on the API (not LISTEN in the web)?

Once Postgres is behind a backend service, Blazor replicas cannot `LISTEN` on the DB without breaking the boundary. The API persists state and **pushes** events over SignalR so every frontend stays in sync — including when circuits sit on different Container App replicas.

This MVP runs **one** API instance (enough for the demo). Scaling the API later needs a SignalR backplane (or Postgres `NOTIFY` → hub bridge) between API replicas.

See [IMPLEMENTATION.md](./IMPLEMENTATION.md) for architecture, sequences, API contract, and Azure notes. Incremental SignalR cost on an existing ACA + API + Postgres stack (EUR): [AZURE-COST-ESTIMATE.md](./AZURE-COST-ESTIMATE.md). Shared protocol index: [../IMPLEMENTATION.md](../IMPLEMENTATION.md).
