# MvpAspireRedis — implementation

Shared kanban board across **two Blazor replicas** using **Redis** for board JSON, drag sessions, and pub/sub. Same coordinator/UI as `MvpServerSync`; only transport and store change.

**Run:** `dotnet run --project AppHost` (Docker required)  
**URLs:** http://127.0.0.1:5280/board (web-a) · http://127.0.0.1:5281/board (web-b)  
**Quick start:** [README.md](./README.md)

---

## When to use

- Multiple containers; UI host talks to Redis directly.
- Keep `IDragSessionHub` / `IBoardStore` contracts; swap implementations.
- Local proof that window A and B on **different processes** still sync.

```mermaid
flowchart TB
  Q{Multiple Blazor replicas?}
  Q -->|Yes + Redis OK| This[MvpAspireRedis]
  Q -->|Yes + board team owns DB| Pg[MvpAspirePostgres]
  Q -->|Single process| Sync[MvpServerSync]
```

---

## What it proves

| Concern | MvpServerSync | MvpAspireRedis |
| --- | --- | --- |
| Board items | In-memory singleton | Redis string `mvp:board:items` |
| Board refresh | Same process event | Pub/sub `mvp:board:changed` |
| Drag events | In-process hub | Pub/sub `mvp:drag:events` |
| Drag session | In-memory dict | Key TTL **2 minutes** |
| Hosting | One process | Redis + **web-a** + **web-b** |

---

## AppHost topology

```mermaid
flowchart TB
  AH[Aspire AppHost]
  AH --> Redis[(Redis)]
  AH --> WA["web-a :5280<br/>MVP_DEMO_REPLICA=A"]
  AH --> WB["web-b :5281<br/>MVP_DEMO_REPLICA=B"]
  WA --> Redis
  WB --> Redis
  UserA[Window 1] -->|/board| WA
  UserB[Window 2] -->|/board| WB
```

### Why web-a / web-b (not `WithReplicas(2)`)

```mermaid
flowchart LR
  subgraph Hard["WithReplicas — one proxied URL"]
    U[Window] --> LB[Load balancer]
    LB --> R0
    LB --> R1
  end
  subgraph Easy["Pinned URLs"]
    W1 --> A[5280 web-a]
    W2 --> B[5281 web-b]
  end
```

Pinned ports force “window 1 → replica A, window 2 → replica B” for demos.

---

## Layers

```mermaid
flowchart TB
  UI["Board.razor"]
  Coord["DragDropCoordinator (scoped)"]
  Hub["RedisDragSessionHub"]
  Store["RedisBoardStore"]
  Redis[(Redis)]

  UI --> Coord
  Coord --> Hub
  Coord --> Store
  Hub <--> Redis
  Store <--> Redis
```

| Piece | Role |
| --- | --- |
| `RedisDragSessionHub` | Local subscriber map + Redis publish/subscribe |
| `RedisBoardStore` | Authoritative JSON + change channel |
| `DragDropCoordinator` | Unchanged gesture logic |

This is **Redis pub/sub + local fan-out**, not Azure SignalR Service and not a SignalR Redis backplane. The Blazor circuit is still browser ↔ whichever web replica owns it.

```mermaid
flowchart TB
  subgraph PerReplica["Each web process"]
    Circuit["Blazor circuit"]
    Local["Local subscriber map"]
    Circuit --> Local
  end
  Bus["Redis pub/sub"]
  Local <--> Bus
```

---

## Hub fan-out

```mermaid
flowchart LR
  subgraph ReplicaA["web-a"]
    CA[Circuits]
    HA[RedisDragSessionHub]
  end
  subgraph ReplicaB["web-b"]
    CB[Circuits]
    HB[RedisDragSessionHub]
  end
  Redis[(Redis)]
  CA --> HA
  HA -->|PUBLISH mvp:drag:events| Redis
  Redis -->|SUBSCRIBE| HB
  HB --> CB
```

---

## End-to-end: drag across replicas

```mermaid
sequenceDiagram
    actor User
    participant WinA as Window → web-a
    participant HubA as Hub A
    participant Redis as Redis
    participant HubB as Hub B
    participant WinB as Window → web-b
    participant Board as mvp:board:items

    User->>WinA: dragstart Box A
    WinA->>HubA: BeginDragAsync
    HubA->>Redis: SET session TTL 2m
    HubA->>Redis: PUBLISH Started
    Redis-->>HubB: Started
    HubB->>WinB: RemoteDrag / highlight

    User->>WinB: drop on Inbox
    WinB->>Board: MoveItem
    WinB->>Redis: PUBLISH board:changed
    Redis-->>HubA: refresh UI
    WinB->>HubB: AcceptDropAsync
    HubB->>Redis: DEL session + PUBLISH DropAccepted
    Redis-->>HubA: DropAccepted
    HubA->>WinA: clear LocalDrag
```

---

## Redis contract

| Key / channel | Role |
| --- | --- |
| `mvp:board:items` | Board JSON |
| `mvp:board:changed` | Refresh notify |
| `mvp:drag:events` | Drag envelopes |
| `mvp:drag:session:{id}` | Source circuit id (TTL 2m) |

```mermaid
flowchart TB
  subgraph Strings
    BI[mvp:board:items]
    DS[mvp:drag:session:id]
  end
  subgraph Channels
    BC[mvp:board:changed]
    DE[mvp:drag:events]
  end
  Move --> BI
  Move --> BC
  Begin --> DS
  Begin --> DE
  Accept --> DS
  Accept --> DE
```

---

## DI

```csharp
builder.AddRedisClient("redis");

builder.Services.AddSingleton<RedisBoardStore>();
builder.Services.AddSingleton<IBoardStore>(sp => sp.GetRequiredService<RedisBoardStore>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<RedisBoardStore>());

builder.Services.AddSingleton<RedisDragSessionHub>();
builder.Services.AddSingleton<IDragSessionHub>(sp => sp.GetRequiredService<RedisDragSessionHub>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<RedisDragSessionHub>());

builder.Services.AddScoped<DragDropCoordinator>();
```

AppHost: `AddRedis("redis")` + `WithReference(redis)` on web-a / web-b.

---

## Production notes

```mermaid
flowchart TB
  RedisFix[Redis fixes shared drag + board]
  Circuit[Blazor circuit affinity<br/>or Azure SignalR Service]
  RedisFix -.- Circuit
```

- Scope keys/channels by tenant/workspace.
- Treat drag sessions as ephemeral (TTL).
- Redis does **not** restore unfinished drags after circuit reconnect.
- Authoritative domain data may move to SQL later; keep “write → notify.”

### Circuit disconnect

```mermaid
sequenceDiagram
    participant Src as Source
    participant Redis as Session key
    participant Tgt as Target
    Src->>Redis: SET TTL 2m
    Src->>Tgt: DragStarted
    Note over Src: circuit drops
    Src--xSrc: no Cancel
    Note over Tgt: hint until TTL / refresh
```

---

## File map

| Path | Role |
| --- | --- |
| `AppHost/AppHost.cs` | Redis + web-a / web-b |
| `Web/Infrastructure/RedisKeys.cs` | Key contract |
| `Web/Services/Board/RedisBoardStore.cs` | Board |
| `Web/Services/DragSession/RedisDragSessionHub.cs` | Drag bus |
| `Web/Services/DragDrop/DragDropCoordinator.cs` | Gesture |
| `Web/Components/Pages/Board.razor` | UI |

Index: [../IMPLEMENTATION.md](../IMPLEMENTATION.md) · Compare: [MvpAspirePostgres](../MvpAspirePostgres/IMPLEMENTATION.md)
