# MvpBroadcastChannel — implementation

Shared **kanban board** where drag signaling stays in the **browser** via `BroadcastChannel` + JSInterop. Each page keeps its own board list and syncs moves with messages.

**Run:** `dotnet run --project MvpBroadcastChannel` → http://localhost:5201/board  
**Constraint:** two side-by-side **windows**, same browser profile.

---

## When to use

- Same browser / profile only (no multi-device drag).
- Minimal latency — no server hop for drag hints.
- Azure scale-out does **not** break drag **signaling** (messages never leave the browser).

```mermaid
flowchart TB
  Q{Same browser only?}
  Q -->|Yes| This[MvpBroadcastChannel]
  Q -->|No / multi-device| Server[Server hub MVPs]
```

---

## Topology

```mermaid
flowchart LR
  subgraph BrowserOnly["Same browser profile"]
    T1[Window A<br/>CrossTabBus]
    T2[Window B<br/>CrossTabBus]
    BC[(BroadcastChannel<br/>cross-tab-dnd-v1)]
    T1 <--> BC
    T2 <--> BC
  end
  T1 -.->|"Blazor circuit<br/>(UI only)"| S1[Any server instance]
  T2 -.->|"Blazor circuit"| S2[Any server instance]
```

Blazor circuits can land on different containers; drag messages still flow client-side.

---

## Layers

```mermaid
flowchart TB
  UI["Board.razor + Draggable / DropZone"]
  Coord["DragDropCoordinator (scoped)"]
  Bus["ICrossTabBus → CrossTabBus"]
  JS["crossTabChannel.js"]
  List["In-page board list"]

  UI --> Coord
  Coord --> Bus
  Bus --> JS
  Coord --> List
  JS -.->|"OnBrowserMessage"| Bus
  Bus -.-> Coord
```

| Piece | Role |
| --- | --- |
| `crossTabChannel.js` | Creates `BroadcastChannel`, `tabId`, publish/subscribe |
| `CrossTabBus` | JSON envelopes; ignores self messages |
| `DragDropCoordinator` | Same gesture protocol as server-hub demos |
| Board list | Per page; `ItemMoved` (or equivalent) keeps lists aligned |

---

## Protocol & state

```mermaid
stateDiagram-v2
  [*] --> Idle
  Idle --> LocalDragging: DragStarted
  Idle --> RemoteHint: DragStarted (other tab)
  LocalDragging --> Idle: DropAccepted / Cancelled
  RemoteHint --> Idle: DropAccepted / Cancelled
```

| Message | Purpose |
| --- | --- |
| DragStarted | Remote hint / enable drop zones |
| DragCancelled | Clear remote hint |
| DropAccepted | Apply move locally on both sides |
| ItemMoved (board sync) | Keep zone membership aligned |

---

## End-to-end sequence

```mermaid
sequenceDiagram
    participant WinA as Window A
    participant BusA as CrossTabBus A
    participant BC as BroadcastChannel
    participant BusB as CrossTabBus B
    participant WinB as Window B

    WinA->>BusA: BeginLocalDrag
    BusA->>BC: DragStarted JSON
    BC->>BusB: message
    BusB->>WinB: RemoteDrag / highlight

    WinB->>WinB: drop → update local list
    WinB->>BusB: DropAccepted + ItemMoved
    BusB->>BC: publish
    BC->>BusA: DropAccepted
    BusA->>WinA: remove/move item, clear LocalDrag
```

### 150 ms cancel grace

Same as server demos: `dragend` waits briefly so `DropAccepted` can win over `DragCancelled`.

```mermaid
sequenceDiagram
    participant Src as Source tab
    participant BC as Channel
    participant Tgt as Target tab
    Tgt->>BC: DropAccepted
    Src->>Src: dragend → 150ms
    BC->>Src: DropAccepted
    Note over Src: skip Cancel
```

---

## DI & startup

```csharp
builder.Services.AddScoped<ICrossTabBus, CrossTabBus>();
builder.Services.AddScoped<DragDropCoordinator>();
```

Start after first interactive render:

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
        await Coordinator.StartAsync();
}
```

Copy `wwwroot/js/crossTabChannel.js` when integrating.

---

## Scale-out & disconnect

```mermaid
flowchart TB
  subgraph DuringReconnect["One circuit down"]
    T1[Tab A — circuit reconnecting]
    T2[Tab B — healthy]
    Chan[(BroadcastChannel)]
    T2 <--> Chan
    T1 -.-x|"no Blazor events"| Server
    T2 -->|"drag msgs still OK"| Chan
  end
```

- **Multi-container:** fine for signaling; persist authoritative data on drop if needed (API).
- **Circuit drop:** browser channel can still deliver between healthy tabs; the down tab cannot participate until reconnect.

---

## File map

| Path | Role |
| --- | --- |
| `Program.cs` | DI |
| `Services/CrossTab/CrossTabBus.cs` | JSInterop bus |
| `wwwroot/js/crossTabChannel.js` | BroadcastChannel |
| `Services/DragDrop/DragDropCoordinator.cs` | Gesture state |
| `Components/Pages/Board.razor` | UI |

See also [../IMPLEMENTATION.md](../IMPLEMENTATION.md) and [MvpCaseSidePanelBroadcast](../MvpCaseSidePanelBroadcast/IMPLEMENTATION.md) (same transport, side-panel domain).
