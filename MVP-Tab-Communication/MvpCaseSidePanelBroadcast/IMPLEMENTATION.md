# MvpCaseSidePanelBroadcast — implementation

Same **case side panel** UX as `MvpCaseSidePanelServer`, but drag signaling uses **BroadcastChannel** in the browser instead of an in-process hub.

**Run:** `dotnet run --project MvpCaseSidePanelBroadcast` → http://localhost:5204/  
**Constraint:** two side-by-side **windows**, same browser profile.

---

## When to use

- Side-panel “move case between windows” + same-browser only.
- Multi-container Blazor hosts without distributing a drag hub.
- Often pair with a **server API commit** on drop (hybrid).

```mermaid
flowchart TB
  Q{Same browser?}
  Q -->|Yes| BC[BroadcastChannel drag hints]
  Q -->|No| Server[Server hub + shared bus]
  BC --> Persist{Authoritative persist?}
  Persist -->|Yes| Hybrid[Drop → API]
  Persist -->|No| Demo[In-memory panels only]
```

---

## Topology

```mermaid
flowchart LR
  subgraph Browser
    WA[Window A<br/>SidePanelState A]
    WB[Window B<br/>SidePanelState B]
    Chan[(BroadcastChannel)]
    WA <--> Chan
    WB <--> Chan
  end
  WA -.-> S1[Blazor instance ?]
  WB -.-> S2[Blazor instance ?]
```

Each window keeps its own `SidePanelState`. Drag messages never need shared server memory.

---

## Layers

```mermaid
flowchart TB
  UI["CaseSidePanel"]
  Coord["DragDropCoordinator"]
  Bus["CrossTabBus + crossTabChannel.js"]
  Panel["SidePanelState (scoped)"]
  Catalog["ICaseCatalog"]

  UI --> Coord
  Coord --> Bus
  Coord --> Panel
  Panel --> Catalog
```

| Piece | Lifetime | Role |
| --- | --- | --- |
| `ICrossTabBus` / `CrossTabBus` | Scoped | Browser pub/sub |
| `SidePanelState` | Scoped | This window’s links |
| `DragDropCoordinator` | Scoped | Gesture + 150 ms grace |

---

## End-to-end sequence

```mermaid
sequenceDiagram
    participant WinA as Window A
    participant BC as BroadcastChannel
    participant WinB as Window B

    WinA->>WinA: Generate Tab A cases
    WinB->>WinB: Generate Tab B cases
    WinA->>BC: DragStarted
    BC->>WinB: RemoteDrag / highlight
    WinB->>WinB: Panel.Add on drop
    WinB->>BC: DropAccepted
    BC->>WinA: DropAccepted
    WinA->>WinA: Panel.Remove(caseId)
```

### Hybrid production (recommended)

```mermaid
sequenceDiagram
    participant WinA as Window A
    participant BC as BroadcastChannel
    participant WinB as Window B
    participant API as Server API / DB

    WinA->>BC: DragStarted
    BC->>WinB: hint
    WinB->>BC: DropAccepted (UI)
    WinB->>API: Persist move (authoritative)
    BC->>WinA: remove from panel
```

---

## DI & startup

```csharp
builder.Services.AddSingleton<ICaseCatalog, CaseCatalog>();
builder.Services.AddScoped<ICrossTabBus, CrossTabBus>();
builder.Services.AddScoped<SidePanelState>();
builder.Services.AddScoped<DragDropCoordinator>();
```

`await Coordinator.StartAsync()` after first interactive render (needs JSInterop).

---

## Scale-out & reconnect

```mermaid
flowchart TB
  subgraph OK["Multi-container"]
    direction LR
    N1[Node 1 circuit] -.-> WA[Window A]
    N2[Node 2 circuit] -.-> WB[Window B]
    WA <--> BC[(BroadcastChannel)]
    WB <--> BC
  end
```

| Concern | Behavior |
| --- | --- |
| Different Blazor replicas | Drag signaling still works (browser) |
| One circuit reconnecting | Other tab can still send/receive channel messages |
| Multi-device / other browser | Not supported — use server hub |

---

## File map

| Path | Role |
| --- | --- |
| `Program.cs` | DI |
| `Services/CrossTab/*` | Bus |
| `wwwroot/js/crossTabChannel.js` | BroadcastChannel |
| `Services/SidePanel/SidePanelState.cs` | Per-window list |
| `Components/SidePanel/CaseSidePanel.razor` | UI |

Compare: [MvpCaseSidePanelServer](../MvpCaseSidePanelServer/IMPLEMENTATION.md), [MvpBroadcastChannel](../MvpBroadcastChannel/IMPLEMENTATION.md). Index: [../IMPLEMENTATION.md](../IMPLEMENTATION.md).
