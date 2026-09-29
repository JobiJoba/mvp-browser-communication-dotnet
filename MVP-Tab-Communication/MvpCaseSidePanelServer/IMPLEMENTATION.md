# MvpCaseSidePanelServer — implementation

**Case-management** style MVP: each window has its **own** managed-cases side panel. Drag **moves** a case link from one panel to the other via an in-process `DragSessionHub`.

**Run:** `dotnet run --project MvpCaseSidePanelServer` → http://localhost:5203/  
**Constraint:** two side-by-side **windows**.

---

## When to use

- Each window owns a working set (not one shared board list).
- Pure C# server hub.
- Closest template for “drag a case into the other window’s panel.”

```mermaid
flowchart LR
  subgraph SidePanel["Per-window state"]
    P1[Window A<br/>SidePanelState]
    P2[Window B<br/>SidePanelState]
  end
  Hub[DragSessionHub]
  P1 <--> Hub
  P2 <--> Hub
  Hub -.->|"DropAccepted → source Remove"| P1
  Hub -.->|"DropAccepted → target Add"| P2
```

Contrast with board demos: board = one shared store; side panel = **scoped** lists + move semantics.

---

## Layers

```mermaid
flowchart TB
  UI["CaseSidePanel + case rows"]
  Coord["DragDropCoordinator (scoped)"]
  Hub["IDragSessionHub (singleton)"]
  Panel["SidePanelState (scoped)"]
  Catalog["ICaseCatalog (singleton seed data)"]

  UI --> Coord
  Coord --> Hub
  Coord --> Panel
  Panel --> Catalog
  Hub -.-> Coord
```

| Piece | Lifetime | Role |
| --- | --- | --- |
| `SidePanelState` | Scoped | Links visible in **this** window |
| `DragDropCoordinator` | Scoped | Local/Remote drag + 150 ms grace |
| `DragSessionHub` | Singleton | Cross-circuit events (one process) |
| `ICaseCatalog` | Singleton | Demo datasets (Tab A / Tab B) |

---

## Demo flow

```mermaid
sequenceDiagram
    actor User
    participant WinA as Window A
    participant Hub as DragSessionHub
    participant WinB as Window B

    User->>WinA: Generate cases for Tab A
    User->>WinB: Generate cases for Tab B
    User->>WinA: dragstart case link
    WinA->>Hub: DragStarted
    Hub->>WinB: RemoteDrag / highlight panel
    User->>WinB: drop on Managed cases
    WinB->>WinB: Panel.Add(link)
    WinB->>Hub: DropAccepted
    Hub->>WinA: DropAccepted
    WinA->>WinA: Panel.Remove(caseId)
```

```mermaid
stateDiagram-v2
  [*] --> Idle
  Idle --> LocalDragging: DragStarted
  Idle --> RemoteHint: DragStarted (other)
  LocalDragging --> Idle: DropAccepted / Cancelled
  RemoteHint --> Idle: DropAccepted / Cancelled
```

---

## Wiring the panel

```csharp
builder.Services.AddSingleton<ICaseCatalog, CaseCatalog>();
builder.Services.AddSingleton<IDragSessionHub, DragSessionHub>();
builder.Services.AddScoped<SidePanelState>();
builder.Services.AddScoped<DragDropCoordinator>();
```

In `CaseSidePanel.razor`:

- `Coordinator.Dispatcher = InvokeAsync`
- Subscribe `Changed` + `LinkRemovedRemotely` / `ItemRemovedRemotely`
- Drop: `RemoteDrag` → `Panel.Add` + `AcceptRemoteDropAsync`; `LocalDrag` → `AcceptLocalDropAsync`
- On remote accept for a link you owned → `Panel.Remove`

```mermaid
flowchart LR
  Drop[ondrop] --> Remote{RemoteDrag?}
  Remote -->|Yes| Add[Panel.Add] --> AccR[AcceptRemoteDrop]
  Remote -->|No| Local{LocalDrag?}
  Local -->|Yes| AccL[AcceptLocalDrop]
```

---

## Scale-out

```mermaid
flowchart TB
  subgraph Single["One process — OK"]
    A1[Window A] --> H1[In-process hub]
    B1[Window B] --> H1
  end
  subgraph Multi["Multi-instance — hub breaks"]
    A2 --> I1[Hub on node 1]
    B2 --> I2[Hub on node 2]
    I1 -.-x I2
  end
```

- Panel state stays scoped (correct).
- Only **drag transport** must be distributed (Redis / Postgres API) **or** switch to BroadcastChannel + API on drop.

Production sketch:

```mermaid
flowchart LR
  UI[Blazor hosts] --> Hub[(Redis or Api SignalR)]
  UI --> API[Persist assignment on drop]
  API --> DB[(Database)]
```

See [MvpAspireRedis](../MvpAspireRedis/IMPLEMENTATION.md), [MvpAspirePostgres](../MvpAspirePostgres/IMPLEMENTATION.md), or [MvpCaseSidePanelBroadcast](../MvpCaseSidePanelBroadcast/IMPLEMENTATION.md).

---

## File map

| Path | Role |
| --- | --- |
| `Program.cs` | DI |
| `Services/DragSession/*` | Hub |
| `Services/DragDrop/DragDropCoordinator.cs` | Gesture |
| `Services/SidePanel/SidePanelState.cs` | Per-window list |
| `Components/SidePanel/CaseSidePanel.razor` | Drop target UI |
| `Services/Cases/*` | Demo catalog |

See [../IMPLEMENTATION.md](../IMPLEMENTATION.md).
