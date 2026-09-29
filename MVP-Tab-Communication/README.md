# Blazor Server Cross-Tab Communication — MVPs

Demos of cross-window drag-and-drop for Blazor Server (.NET 10).

## Projects

| Project | What it demos | Transport | Port |
| --- | --- | --- | --- |
| [`MvpBroadcastChannel/`](MvpBroadcastChannel/) | Shared board boxes | `BroadcastChannel` + JSInterop | http://localhost:5201 |
| [`MvpServerSync/`](MvpServerSync/) | Shared board boxes | In-process hub (no JSInterop) | http://localhost:5202 |
| [`MvpCaseSidePanelServer/`](MvpCaseSidePanelServer/) | Per-tab managed-case side panel | In-process hub (no JSInterop) | http://localhost:5203 |
| [`MvpCaseSidePanelBroadcast/`](MvpCaseSidePanelBroadcast/) | Per-tab managed-case side panel | `BroadcastChannel` + JSInterop | http://localhost:5204 |

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

## When to pick which transport

**Prefer server hub when:**

- You want pure C# / Blazor Server patterns.
- You may later sync across users or machines (swap the singleton for SignalR + Redis).
- You want a server-side audit point for transfers.

**Prefer BroadcastChannel when:**

- Same-browser only is enough.
- You want minimal latency and no server hop for drag messages.
