# MvpAspirePostgres — implementation

Shared kanban board with a **backend Api** that owns **PostgreSQL**, plus two Blazor **frontends** that call REST and receive SignalR push. Models “board team owns the service.”

**Run:** `dotnet run --project AppHost` (Docker required)  
**URLs:** web-a http://127.0.0.1:5290/board · web-b http://127.0.0.1:5291/board · api http://127.0.0.1:5295/api/board  
**Quick start:** [README.md](./README.md)

---

## When to use

- Microservices / team boundary: UI must not open the DB.
- Multiple frontend replicas on Azure Container Apps.
- Prefer Postgres you already run for domain data.

```mermaid
flowchart TB
  Need[Need shared board across<br/>frontend replicas?]
  Need --> Boundary{Who owns the DB?}
  Boundary -->|Backend / board team| This[MvpAspirePostgres]
  Boundary -->|Same process as UI| Other[In-process or other MVPs]
```

---

## Topology

```mermaid
flowchart LR
  WA[web-a :5290] -->|HTTP + SignalR| API[api :5295]
  WB[web-b :5291] -->|HTTP + SignalR| API
  API --> PG[(PostgreSQL mvpdb)]
```

```mermaid
flowchart TB
  AH[Aspire AppHost]
  AH --> PG[(postgres / mvpdb)]
  AH --> API[api]
  AH --> WA[web-a]
  AH --> WB[web-b]
  API --> PG
  WA --> API
  WB --> API
```

Frontends never take a Postgres connection string. Demo uses **one** Api instance.

---

## Why SignalR on the Api (not LISTEN in Web)

```mermaid
flowchart LR
  subgraph Bad["Breaks team boundary"]
    W1[Web] -->|LISTEN| DB[(Postgres)]
    W2[Web] -->|LISTEN| DB
  end
  subgraph Good["This MVP"]
    F1[Web] -->|HTTP/SignalR| Api
    F2[Web] -->|HTTP/SignalR| Api
    Api --> DB2[(Postgres)]
  end
```

Once the DB is behind a service, only that service should notify. Api persists, then pushes `BoardChanged` / `DragEvent` to all frontends.

---

## What it proves

| Concern | How this MVP handles it |
| --- | --- |
| Board items | Api → table `board_items` |
| Board UI refresh across frontends | SignalR `BoardChanged` |
| Drag events across frontends | SignalR `DragEvent` |
| In-flight drag session | Table `drag_sessions` with `expires_at` (~2 min) |
| Hosting | AppHost: Postgres + **api** + **web-a** + **web-b** |
| Demo URLs | web **5290 / 5291**, api **5295** |

---

## Layers

```mermaid
flowchart TB
  subgraph Frontends["Web replicas"]
    UI[Board.razor]
    Coord[DragDropCoordinator]
    Store[ApiBoardStore]
    Hub[ApiDragSessionHub]
    RT[ApiRealtimeConnection]
    HTTP[BoardApiClient]
    UI --> Coord
    Coord --> Hub
    Coord --> Store
    Store --> HTTP
    Hub --> HTTP
    Store --> RT
    Hub --> RT
  end
  subgraph Backend["Api"]
    BS[BoardService]
    DS[DragSessionService]
    SR[BoardRealtimeHub]
    BS --> SR
    DS --> SR
  end
  HTTP -->|REST| BS
  HTTP -->|REST| DS
  RT <-->|SignalR| SR
  BS --> PG[(Postgres)]
  DS --> PG
```

| Project | Types |
| --- | --- |
| **Api** | `BoardService`, `DragSessionService`, `BoardRealtimeHub`, schema |
| **Web** | `BoardApiClient`, `ApiRealtimeConnection`, `ApiBoardStore`, `ApiDragSessionHub` |

---

## API surface

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/api/board` | List items |
| POST | `/api/board/move` | `{ itemId, zoneId }` |
| POST | `/api/drag/begin` | Start session |
| POST | `/api/drag/cancel` | Cancel |
| POST | `/api/drag/accept` | Accept drop |
| SignalR | `/hubs/board` | `BoardChanged`, `DragEvent` |

```mermaid
flowchart LR
  Client[Web] -->|POST /api/drag/begin| Api
  Api -->|INSERT drag_sessions| PG[(Postgres)]
  Api -->|Clients.All DragEvent| Hub[SignalR]
  Hub --> Client
  Hub --> Other[Other web replica]
```

### Schema

```mermaid
erDiagram
  BOARD_ITEMS {
    text id PK
    text title
    text color
    text zone_id
  }
  DRAG_SESSIONS {
    text session_id PK
    text source_circuit_id
    timestamptz expires_at
  }
```

---

## End-to-end: drag across frontends

```mermaid
sequenceDiagram
    actor User
    participant WinA as web-a circuit
    participant Api as Api
    participant PG as Postgres
    participant WinB as web-b circuit

    User->>WinA: dragstart
    WinA->>Api: POST /api/drag/begin
    Api->>PG: INSERT drag_sessions
    Api-->>WinA: SignalR DragEvent Started
    Api-->>WinB: SignalR DragEvent Started
    WinB->>WinB: RemoteDrag / highlight

    User->>WinB: drop Inbox
    WinB->>Api: POST /api/board/move
    Api->>PG: UPDATE board_items
    Api-->>WinA: SignalR BoardChanged
    Api-->>WinB: SignalR BoardChanged
    WinB->>Api: POST /api/drag/accept
    Api->>PG: DELETE session
    Api-->>WinA: SignalR DropAccepted
    WinA->>WinA: clear LocalDrag
```

---

## DI

**Api** — register services on the builder, map the hub on the app:

```csharp
builder.AddNpgsqlDataSource("mvpdb");
builder.Services.AddSignalR();
builder.Services.AddSingleton<BoardService>();
builder.Services.AddSingleton<DragSessionService>();

var app = builder.Build();
// ...
app.MapHub<BoardRealtimeHub>("/hubs/board");
```

`MapHub` is a pipeline mapping (like `MapGet`), not a DI registration — it must run on `WebApplication` after `Build()`, which is what `Api/Program.cs` does.

**Web**

```csharp
builder.Services.AddHttpClient<BoardApiClient>(c =>
    c.BaseAddress = new Uri("https+http://api"));

builder.Services.AddSingleton<ApiRealtimeConnection>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ApiRealtimeConnection>());

builder.Services.AddSingleton<ApiBoardStore>();
builder.Services.AddSingleton<IBoardStore>(sp => sp.GetRequiredService<ApiBoardStore>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<ApiBoardStore>());

builder.Services.AddSingleton<ApiDragSessionHub>();
builder.Services.AddSingleton<IDragSessionHub>(sp => sp.GetRequiredService<ApiDragSessionHub>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<ApiDragSessionHub>());
```

Hosted order: realtime connection first, then store/hub subscribers.

---

## Production / Azure

```mermaid
flowchart TB
  FE[Container App — Blazor replicas]
  API[Container App — Board Api]
  DB[(Azure Database for PostgreSQL)]
  FE -->|HTTP + SignalR| API
  API --> DB
  FE -.->|sticky or Azure SignalR| Circuits[Blazor circuits]
```

| Topic | Guidance |
| --- | --- |
| Frontend scale-out | Works with one Api; SignalR fans out to all webs |
| Api scale-out | Needs SignalR backplane (or NOTIFY→hub bridge) between Api replicas |
| Blazor circuits | Sticky sessions or Azure SignalR Service |
| Channel scope | Tenant/workspace in production |
| Cost ballpark | [AZURE-COST-ESTIMATE.md](./AZURE-COST-ESTIMATE.md) — SignalR incremental ≈ €0 (hub on API) or ≈ €45–50/mo (Azure SignalR Standard 1 unit) |

```mermaid
flowchart LR
  subgraph ThisDemo["MVP"]
    OneApi[Single Api]
  end
  subgraph Later["Scale Api"]
    A1[Api 1] <--> BP[SignalR backplane]
    A2[Api 2] <--> BP
  end
```

---

## File map

| Path | Role |
| --- | --- |
| `AppHost/AppHost.cs` | Postgres + api + web-a/b |
| `Api/Services/BoardService.cs` | Board + SignalR notify |
| `Api/Services/DragSessionService.cs` | Sessions + SignalR |
| `Api/Hubs/BoardRealtimeHub.cs` | Push hub |
| `Api/Infrastructure/PostgresSchema.cs` | Tables |
| `Web/Services/Api/BoardApiClient.cs` | HTTP |
| `Web/Services/Api/ApiRealtimeConnection.cs` | SignalR client |
| `Web/Services/Board/ApiBoardStore.cs` | `IBoardStore` |
| `Web/Services/DragSession/ApiDragSessionHub.cs` | `IDragSessionHub` |

Index: [../IMPLEMENTATION.md](../IMPLEMENTATION.md)
