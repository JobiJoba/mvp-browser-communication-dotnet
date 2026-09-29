# Cross-window drag-and-drop — guide index

Blazor Server keeps UI state per **circuit**. HTML5 drag runs in the browser. Cross-window drag needs an explicit **session protocol** plus a transport.

```mermaid
flowchart TB
  subgraph Browser
    WA[Window A]
    WB[Window B]
  end
  subgraph Server
    CA[Circuit A]
    CB[Circuit B]
  end
  WA <--> CA
  WB <--> CB
  CA -.-x|"no shared drag<br/>by default"| CB
```

**Protocol:** `DragStarted` → `DropAccepted` or `DragCancelled`  
**Demo rule:** two side-by-side **windows** (tabs usually cancel HTML5 drag).

```mermaid
stateDiagram-v2
  [*] --> Idle
  Idle --> LocalDragging: DragStarted (source)
  Idle --> RemoteHint: DragStarted (other)
  LocalDragging --> Idle: DropAccepted / Cancelled
  RemoteHint --> Idle: DropAccepted / Cancelled
```

---

## Pick an MVP

| Project | Transport | Shared state | Doc |
| --- | --- | --- | --- |
| [MvpServerSync](./MvpServerSync/) | In-process hub | Shared `BoardStore` | [IMPLEMENTATION](./MvpServerSync/IMPLEMENTATION.md) |
| [MvpBroadcastChannel](./MvpBroadcastChannel/) | `BroadcastChannel` | Per-page board + messages | [IMPLEMENTATION](./MvpBroadcastChannel/IMPLEMENTATION.md) |
| [MvpAspireRedis](./MvpAspireRedis/) | Redis pub/sub | Redis board + sessions | [IMPLEMENTATION](./MvpAspireRedis/IMPLEMENTATION.md) |
| [MvpAspirePostgres](./MvpAspirePostgres/) | HTTP + SignalR → Api | Postgres via Api | [IMPLEMENTATION](./MvpAspirePostgres/IMPLEMENTATION.md) |
| [MvpAspireMessages](./MvpAspireMessages/) | HTTP → Api | In-memory messages | [IMPLEMENTATION](./MvpAspireMessages/IMPLEMENTATION.md) |
| [MvpCaseSidePanelServer](./MvpCaseSidePanelServer/) | In-process hub | Per-window `SidePanelState` | [IMPLEMENTATION](./MvpCaseSidePanelServer/IMPLEMENTATION.md) |
| [MvpCaseSidePanelBroadcast](./MvpCaseSidePanelBroadcast/) | `BroadcastChannel` | Per-window panel | [IMPLEMENTATION](./MvpCaseSidePanelBroadcast/IMPLEMENTATION.md) |

```mermaid
flowchart LR
  SS[MvpServerSync] --> InProc[In-process hub]
  CS[MvpCaseSidePanelServer] --> InProc
  AR[MvpAspireRedis] --> Redis[Redis]
  AP[MvpAspirePostgres] --> Api[Api + Postgres]
  AM[MvpAspireMessages] --> MsgApi[Api + in-memory]
  BC[MvpBroadcastChannel] --> BChan[BroadcastChannel]
  CB[MvpCaseSidePanelBroadcast] --> BChan
```

| Pattern | Use when |
| --- | --- |
| In-process hub | Single instance / sticky sessions |
| Redis hub | Multi-replica UI host talks to Redis |
| Postgres Api | Board owned by a backend service |
| Messages Api | List/detail state scenarios (no DB yet) |
| BroadcastChannel | Same browser profile only |

Quick runs and ports: [README.md](./README.md).

---

## Shared building blocks

```mermaid
flowchart TB
  UI[UI components]
  Coord[DragDropCoordinator scoped]
  Transport[IDragSessionHub or ICrossTabBus]
  Domain[IBoardStore or SidePanelState]
  UI --> Coord
  Coord --> Transport
  Coord --> Domain
```

- **Coordinator** — `LocalDrag` / `RemoteDrag`, 150 ms grace so `DropAccepted` wins over `DragCancelled`.
- **Always** set `Coordinator.Dispatcher = InvokeAsync` before `Start` / `StartAsync`.

```mermaid
sequenceDiagram
    participant Src as Source
    participant T as Transport
    participant Tgt as Target
    Src->>T: DragStarted
    Tgt->>T: DropAccepted
    Src->>Src: dragend → 150ms
    T->>Src: DropAccepted
    Note over Src: skip Cancel
```

**Board vs side panel**

```mermaid
flowchart LR
  subgraph Board
    B1 --- S[(One store)]
    B2 --- S
  end
  subgraph Panel
    P1[Own list] -.->|DropAccepted remove| P2[Own list]
  end
```

---

## Multi-instance / Azure (decision)

```mermaid
flowchart TB
  Start{Same browser only?}
  Start -->|Yes| BC{Server commit on drop?}
  Start -->|No| Server[Server hub]
  BC -->|No| E[BroadcastChannel]
  BC -->|Yes| D[BroadcastChannel + API]
  Server --> Scale{Multiple containers?}
  Scale -->|No / sticky| A[In-process hub]
  Scale -->|Yes + Redis| C[MvpAspireRedis]
  Scale -->|Yes + Api/DB| P[MvpAspirePostgres]
```

| Approach | Idea |
| --- | --- |
| Sticky / ARR | Keep circuits on one node — short-term only |
| Azure SignalR Service | Scales **circuits**; does not replace shared drag store |
| Redis / Postgres Api | Shared drag + board across UI replicas |
| BroadcastChannel + API | Client hints; authoritative write on drop |

**Do not confuse:** Blazor circuit SignalR (UI) vs drag transport (hub / Redis / Api SignalR / BroadcastChannel).

```mermaid
flowchart LR
  Blazor[Blazor circuit] --> UI[ondrag / ondrop]
  Drag[Drag transport] --> Coord[Coordinators]
```

Circuit drop mid-drag → new scoped coordinator on reconnect; use TTL / cleanup for ghost “Remote drag active.” Details in each MVP doc.

---

## Checklist

1. Same-browser only → BroadcastChannel path; else server hub + shared bus/store.
2. Multi-replica Blazor → Redis or Postgres Api (not in-process hub alone).
3. Authoritative state off process memory.
4. Sticky sessions or Azure SignalR for circuits.
5. Prove with two windows on two instances early (`web-a` / `web-b`).
