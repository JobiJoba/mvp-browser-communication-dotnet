# MvpServerSync — implementation

In-process server hub for a **shared kanban board**. Both browser windows talk to **one** Blazor Server process; drag events and board state live in memory.

**Run:** `dotnet run --project MvpServerSync` → http://localhost:5202/board  
**Constraint:** two side-by-side **windows** (not tabs).

---

## When to use

- Single app instance (or sticky sessions to one instance).
- Pure C# — no JSInterop for drag signaling.
- Starting point before Redis / API scale-out.

```mermaid
flowchart TB
  Start{Need multi-container<br/>without affinity?}
  Start -->|No| This[MvpServerSync]
  Start -->|Yes| Other[See MvpAspireRedis<br/>or MvpAspirePostgres]
```

---

## Problem & protocol

Each window has its own Blazor circuit. Nothing shares drag state by default.

```mermaid
flowchart TB
  subgraph Browser
    WA[Window A]
    WB[Window B]
  end
  subgraph OneProcess["Single app process"]
    CA[Circuit A]
    CB[Circuit B]
    Hub[DragSessionHub]
    Store[BoardStore]
  end
  WA <--> CA
  WB <--> CB
  CA --> Hub
  CB --> Hub
  CA --> Store
  CB --> Store
```

| Message | Meaning |
| --- | --- |
| DragStarted | Other windows show remote-drag UI |
| DragCancelled | Clear remote hint (no valid drop) |
| DropAccepted | Target applied move; source clears local drag |

```mermaid
stateDiagram-v2
  [*] --> Idle
  Idle --> LocalDragging: DragStarted (source)
  Idle --> RemoteHint: DragStarted (other)
  LocalDragging --> Idle: DropAccepted / Cancelled
  RemoteHint --> Idle: DropAccepted / Cancelled
```

---

## Layers

```mermaid
flowchart TB
  UI["Board.razor + Draggable / DropZone"]
  Coord["DragDropCoordinator (scoped)"]
  Hub["IDragSessionHub → DragSessionHub (singleton)"]
  Store["IBoardStore → BoardStore (singleton)"]

  UI --> Coord
  Coord --> Hub
  Coord --> Store
  Hub -.->|"BroadcastAsync"| Coord
  Store -.->|"Changed"| UI
```

| Piece | Lifetime | Role |
| --- | --- | --- |
| `DragDropCoordinator` | Scoped (per circuit) | `LocalDrag` / `RemoteDrag`, 150 ms cancel grace |
| `DragSessionHub` | Singleton | In-process pub/sub |
| `BoardStore` | Singleton | Shared box list + zones |

---

## End-to-end sequence

```mermaid
sequenceDiagram
    participant WinA as Window A
    participant CoordA as Coordinator A
    participant Hub as DragSessionHub
    participant CoordB as Coordinator B
    participant WinB as Window B
    participant Store as BoardStore

    WinA->>CoordA: dragstart
    CoordA->>Hub: BeginDragAsync
    Hub->>CoordB: DragStarted
    CoordB->>WinB: highlight drop zones

    WinB->>CoordB: drop on Inbox
    CoordB->>Store: MoveItem
    Store-->>WinA: Changed (re-render)
    Store-->>WinB: Changed
    CoordB->>Hub: AcceptDropAsync
    Hub->>CoordA: DropAccepted
    CoordA->>WinA: clear LocalDrag
```

### Cancel vs drop race

```mermaid
sequenceDiagram
    participant Src as Source
    participant Hub as Hub
    participant Tgt as Target
    Src->>Hub: DragStarted
    Tgt->>Hub: DropAccepted
    Src->>Src: dragend → wait 150ms
    Hub->>Src: DropAccepted (originCompleted)
    Note over Src: skip Cancel
```

---

## DI (`Program.cs`)

```csharp
builder.Services.AddSingleton<IBoardStore, BoardStore>();
builder.Services.AddSingleton<IDragSessionHub, DragSessionHub>();
builder.Services.AddScoped<DragDropCoordinator>();
```

Wire the page:

```csharp
Coordinator.Dispatcher = InvokeAsync;
Coordinator.Start();
Coordinator.Changed += () => InvokeAsync(StateHasChanged);
```

---

## Scale-out limit

```mermaid
flowchart LR
  subgraph Broken["Two instances — broken"]
    A1[Window A] --> I1[Instance 1 hub]
    B1[Window B] --> I2[Instance 2 hub]
    I1 -.-x I2
  end
```

In-process hub + singleton store **do not** cross containers. Next steps: [MvpAspireRedis](../MvpAspireRedis/IMPLEMENTATION.md) or [MvpAspirePostgres](../MvpAspirePostgres/IMPLEMENTATION.md).

---

## File map

| Path | Role |
| --- | --- |
| `Program.cs` | DI |
| `Services/DragSession/DragSessionHub.cs` | Transport |
| `Services/DragDrop/DragDropCoordinator.cs` | Per-circuit gesture state |
| `Services/Board/BoardStore.cs` | Shared board |
| `Components/Pages/Board.razor` | UI |
| `Components/DragDrop/*` | Draggable / DropZone |

See also [../IMPLEMENTATION.md](../IMPLEMENTATION.md) (index) and [../README.md](../README.md).
