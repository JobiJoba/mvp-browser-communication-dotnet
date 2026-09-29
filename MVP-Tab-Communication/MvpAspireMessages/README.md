# MvpAspireMessages — messages list / detail (Aspire + Api + Blazor)

Foundation MVP for several navigation / state scenarios in one Aspire app:

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

1. Open overview → click row (same tab) → change state → back.
2. Overview remounts and reloads the whole list.

## Scenario 2 — `/messages-cache` (cache + new tab + BroadcastChannel)

Circuit-scoped `MessagesListCache` on the overview tab. Detail opens with `target="_blank"` (new browser tab = new Blazor circuit).

1. Keep overview open.
2. Click a row → detail opens in a **new tab**.
3. Change state → **Save** → `PUT` Api → `BroadcastChannel` notifies the overview → overview **patches one row** (no full list GET) → detail tries `window.close()`.
4. **Cancel** → close only (no Api write, no broadcast).

Yes — the originating overview tab can refresh: not via shared DI (circuits don’t share scoped services), but via **same-browser `BroadcastChannel`**. `window.close()` may be blocked by the browser; if so, close the detail tab manually — the overview patch still happened.

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
   - **Web (reload):** http://127.0.0.1:5300/messages  
   - **Web (cache):** http://127.0.0.1:5300/messages-cache  
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
| `Web/Services/MessagesListCache.cs` | Circuit-scoped list cache (scenario 2) |
| `Web/Services/MessagesCacheTabBus.cs` | BroadcastChannel bridge (scenario 2) |
| `Web/wwwroot/js/messagesCacheChannel.js` | Browser BroadcastChannel + `closeTab` |
| `Web/Components/Pages/Messages.razor` | Scenario 1 overview |
| `Web/Components/Pages/MessageDetail.razor` | Scenario 1 detail |
| `Web/Components/Pages/MessagesCache.razor` | Scenario 2 overview |
| `Web/Components/Pages/MessageCacheDetail.razor` | Scenario 2 detail (Save / Cancel) |
