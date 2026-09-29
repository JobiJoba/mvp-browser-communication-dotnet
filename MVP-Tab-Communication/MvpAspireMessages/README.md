# MvpAspireMessages — messages list / detail (Aspire + Api + Blazor)

Foundation MVP for several navigation / state scenarios in one Aspire app.

**Compare all three routes (advantages, Azure, PWA, serialization):** [SCENARIOS.md](./SCENARIOS.md)

| Layer | Project | Role |
| --- | --- | --- |
| **Backend** | `Api/` | In-memory message store; REST list / get / update state |
| **Frontend** | `Web/` | Blazor Server overview + detail |
| **Hosting** | `AppHost/` | **api** + **web** |

```text
web ──HTTP──► api (in-memory messages)
```

## Scenario 1 — `/messages` (full list reload)

No list cache. Leaving the page disposes the component; coming back (browser **Back** or **Back to overview**) remounts it and always re-runs `OnInitializedAsync` → full `GET /api/messages`.

## Scenario 2 — `/messages-cache` (cache + new tab + BroadcastChannel)

Circuit-scoped cache. Detail opens in a **new tab**. **Save** → `PUT` Api → browser **`BroadcastChannel`** → overview patches one row → `window.close()`. No Azure extras for the notify path (same browser only).

## Scenario 3 — `/messages-cache-server` (cache + new tab + C# bus)

Same UX as scenario 2, but notify is **server-side C#**: singleton `MessagesServerSyncBus` fans out to every subscribed Blazor circuit on **this Web process** (same pattern as `MvpServerSync` drag hub). JS is only used to close the detail tab.

| | Scenario 2 | Scenario 3 |
| --- | --- | --- |
| Notify | `BroadcastChannel` (browser) | `MessagesServerSyncBus` (in-process) |
| Reach | Same browser profile | All circuits on this Web instance |
| Scale-out Web | Still local to browser | Needs SignalR/Redis backplane |
| Azure extras for notify | None | None (single Web node) |

## Prerequisites

- .NET 10 SDK
- Aspire packages `13.5.4` (see AppHost csproj)

No Docker required for this MVP (in-memory store).

## Run

```bash
cd MvpAspireMessages
dotnet run --project AppHost
```

1. Open the Aspire dashboard.
2. Fixed URLs:
   - **Reload:** http://127.0.0.1:5300/messages  
   - **Cache + BroadcastChannel:** http://127.0.0.1:5300/messages-cache  
   - **Cache + server bus:** http://127.0.0.1:5300/messages-cache-server  
   - **Api list JSON:** http://127.0.0.1:5305/api/messages  

## API surface

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/api/messages` | List messages |
| `GET` | `/api/messages/{id}` | Message detail |
| `PUT` | `/api/messages/{id}/state` | `{ "state": "Waiting" \| "Processed" \| "Deleted" }` |

## Project layout

| Path | Role |
| --- | --- |
| `Api/Services/MessageStore.cs` | Seeded in-memory messages |
| `Web/Services/MessagesApiClient.cs` | Typed HTTP client (`https+http://api`) |
| `Web/Services/MessagesListCache.cs` | Circuit-scoped list cache (scenarios 2–3) |
| `Web/Services/MessagesCacheTabBus.cs` | BroadcastChannel bridge (scenario 2) |
| `Web/Services/MessagesServerSyncBus.cs` | In-process cross-circuit bus (scenario 3) |
| `Web/Services/TabCloser.cs` | `window.close()` helper (scenario 3) |
| `Web/wwwroot/js/messagesCacheChannel.js` | BroadcastChannel + close (scenario 2) |
| `Web/wwwroot/js/tabActions.js` | close only (scenario 3) |
| `Web/Components/Pages/Messages.razor` | Scenario 1 overview |
| `Web/Components/Pages/MessageDetail.razor` | Scenario 1 detail |
| `Web/Components/Pages/MessagesCache.razor` | Scenario 2 overview |
| `Web/Components/Pages/MessageCacheDetail.razor` | Scenario 2 detail |
| `Web/Components/Pages/MessagesCacheServer.razor` | Scenario 3 overview |
| `Web/Components/Pages/MessageCacheServerDetail.razor` | Scenario 3 detail |
