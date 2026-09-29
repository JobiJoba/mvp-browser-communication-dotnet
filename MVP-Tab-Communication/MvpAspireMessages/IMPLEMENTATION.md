# MvpAspireMessages — implementation notes

## Architecture

```mermaid
flowchart LR
  Browser[Browser]
  Web[Blazor Web]
  Api[Messages Api]
  Store[(In-memory MessageStore)]

  Browser --> Web
  Web -->|HTTP| Api
  Api --> Store
```

## Flow

```mermaid
sequenceDiagram
  participant U as User
  participant O as Messages overview
  participant D as Message detail
  participant A as Api

  U->>O: Open /messages
  O->>A: GET /api/messages
  A-->>O: list + states
  U->>D: Click row
  D->>A: GET /api/messages/{id}
  U->>D: Change state
  D->>A: PUT /api/messages/{id}/state
  U->>O: Back to overview
  O->>A: GET /api/messages
```

## States

| Enum | UI label |
| --- | --- |
| `Waiting` | In waiting |
| `Processed` | Processed |
| `Deleted` | Deleted |

Persistence is process-local: restarting the Api reseeds the five sample messages.

## Ports

| Resource | URL |
| --- | --- |
| web | http://127.0.0.1:5300 |
| api | http://127.0.0.1:5305 |
