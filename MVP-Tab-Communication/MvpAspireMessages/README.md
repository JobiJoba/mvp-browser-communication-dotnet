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

## Scenario 1 (shipped)

1. Open **Messages** overview — each row shows subject + state badge (`In waiting` / `Processed` / `Deleted`).
2. Click a row → detail page.
3. Change state with the action buttons.
4. **Back to overview** returns to the list (which reloads from the API).

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
   - **Web:** http://127.0.0.1:5300/messages  
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
| `Api/Models/MessageModels.cs` | DTO + state enum |
| `Web/Services/MessagesApiClient.cs` | Typed HTTP client (`https+http://api`) |
| `Web/Components/Pages/Messages.razor` | Overview list |
| `Web/Components/Pages/MessageDetail.razor` | Detail + state change + back |

## Why this shell?

Later scenarios (browser back vs explicit navigate, stale circuit state, multi-tab refresh, etc.) can reuse the same Api + Web + AppHost without standing up a new solution each time.
