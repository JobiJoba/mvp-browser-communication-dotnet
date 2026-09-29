# Incremental cost of SignalR realtime (EUR)

**Question answered:** You already run:

1. **Frontend** on Azure Container Apps  
2. **Backend API** on Azure Container Apps  
3. **PostgreSQL** used by that backend  

What is the **extra Azure cost** of adding a **SignalR connection for realtime updates**, compared to the **same project without SignalR**?

Replica counts, frontend sizing, and Postgres SKU are **out of scope** — treat them as already paid.

**Not a quote.** Indicative **EUR / West Europe (or similar EU region)**, mid–late 2026. Confirm in the [Azure Pricing Calculator](https://azure.microsoft.com/pricing/calculator/) with currency = EUR.

Related: [IMPLEMENTATION.md](./IMPLEMENTATION.md) · [README.md](./README.md)

---

## Baseline (already paid — €0 in this comparison)

```text
Browser ──► Frontend (Container Apps) ──HTTP──► Backend API (Container Apps) ──► PostgreSQL
```

| Component | On the bill today? | Counted in the delta below? |
| --- | --- | --- |
| Frontend Container Apps | Yes | **No** |
| Backend API Container Apps | Yes | **No** |
| Azure Database for PostgreSQL | Yes | **No** |

Without SignalR, clients poll or refresh after REST calls. With SignalR, the backend **pushes** events (as in this MVP: `BoardChanged`, `DragEvent`).

---

## What “adding SignalR” can mean

Two ways to wire the same realtime feature:

```text
Option 1 — Hub on your existing API (self-hosted)
  Frontend ──SignalR──► Backend API (MapHub) ──► PostgreSQL
  Extra Azure product: none

Option 2 — Azure SignalR Service (managed)
  Frontend ──SignalR──► Azure SignalR Service ◄── Backend API ──► PostgreSQL
  Extra Azure product: SignalR Service units (+ messages over quota)
  Requires Microsoft.Azure.SignalR SDK on the API
```

This MVP uses **Option 1** locally (hub on the Api). Option 2 is what you add when you want a managed service, SLA, or a backplane across **multiple API replicas**.

### Clients in this architecture

In `MvpAspirePostgres`, SignalR clients are the **web server replicas** (`ApiRealtimeConnection`), not browsers. Connection count ≈ number of frontend containers (tiny). Each `Clients.All` broadcast is counted once **per connected replica**, so **message quota** matters more than the “1 000 connections/unit” marketing number.

Blazor’s own circuit SignalR (browser ↔ frontend) is a separate concern and is not priced here.

---

## Extra cost vs the same project without SignalR

| Approach | Extra Azure spend ≈ / month | When it fits |
| --- | --- | --- |
| **No SignalR** (REST only) | **€0** | Baseline |
| **Option 1 — SignalR hub on the existing API** | **≈ €0** | One API instance (or sticky routing); light push traffic fits current CPU/RAM |
| **Option 1 + noticeable load** (may need more API vCPU/memory) | **≈ €0–25** | Chatty drag/board fan-out; only if you actually scale the API container up |
| **Option 2 — Azure SignalR Free** | **€0** | Dev / tiny demo: **20** concurrent connections, **20 000** messages/day; **no SLA** |
| **Option 2 — Azure SignalR Standard, 1 unit** | **≈ €45–50** | Production realtime: **1 000** connections/unit, **1 000 000** messages/unit/day included |
| **Option 2 — Standard, 2 units** | **≈ €90–100** | More concurrent connections / message headroom |
| **Messages beyond included quota** | **≈ €0.90–1.00 per million** | Only after the daily included messages per unit |

### Direct answer

| Comparison | Extra cost |
| --- | --- |
| Same stack **with self-hosted SignalR on the existing API** vs **without SignalR** | **Essentially €0 / month** on Azure (no new resource). You pay only if the hub forces you to give the API more compute. |
| Same stack **with Azure SignalR Service Standard (1 unit)** vs **without SignalR** | **≈ €45–50 / month**, plus overage messages if you exceed 1M messages/unit/day. |

That **≈ €45–50** is the only **dedicated** line item for “realtime as a service.” Frontend, API, and Postgres do not get a second bill for existing capacity.

---

## Rates used (indicative EUR)

| Meter | Approx |
| --- | --- |
| Azure SignalR **Free** | **€0** (no SLA) |
| Azure SignalR **Standard** | ~**€1.45–1.55 / unit-day** → **≈ €45–50 / unit-month** (~30.5 days) |
| Azure SignalR **Premium** | ~**€1.80–2.00 / unit-day** → **≈ €55–60 / unit-month** (same quotas as Standard per unit; adds AZ / autoscale / geo features) |
| Extra messages (Standard/Premium) | ~**€0.90–1.00 / million** after the included 1M/unit/day |

USD list price is often cited around **$1.61 / Standard unit-day** (~**$49 / month**). EUR figures above apply a typical EU-region / FX conversion; your portal may show slightly different list prices for West Europe / France Central / North Europe.

Official page: [Azure SignalR Service pricing](https://azure.microsoft.com/pricing/details/signalr-service/).

---

## Side-by-side (same project)

```text
WITHOUT SignalR                          WITH SignalR (self-hosted on API)
─────────────────                        ─────────────────────────────────
Frontend ACA          (paid)             Frontend ACA          (paid — same)
Backend API           (paid)             Backend API + hub     (paid — same bill*)
PostgreSQL            (paid)             PostgreSQL            (paid — same)
                                         *maybe +€0–25 if you enlarge the API

                                         WITH Azure SignalR Service (1 Standard unit)
                                         ───────────────────────────────────────────
                                         Frontend / API / Postgres   (paid — same)
                                         + Azure SignalR Service     ≈ €45–50 / mo
                                         (+ Microsoft.Azure.SignalR on the API)
```

| | Without SignalR | + Hub on existing API | + Azure SignalR Standard 1 unit |
| --- | --- | --- | --- |
| New Azure resource | — | None | SignalR Service |
| Extra € / month | **0** | **≈ 0** (or small API bump) | **≈ 45–50** |
| SLA on push fabric | N/A | Your API uptime | 99.9% (Standard); Free has none |
| Multi-API-replica fan-out | N/A | Needs a backplane | Built-in role of the service |
| Binding clients (this MVP) | N/A | 1 connection per web replica | Same; message volume scales with replicas × events |

---

## What does *not* change the delta

- Frontend Container Apps size / replica count — already excluded.  
- Postgres SKU — board tables are negligible vs instance price; no separate “SignalR DB” charge.  
- Blazor circuit SignalR (browser ↔ frontend) — different concern; this doc is about **API → web replicas realtime push** for board/drag (or equivalent) updates.

---

## Practical recommendation for this MVP shape

1. **Prototype / single API replica:** host SignalR on the existing backend (**≈ €0** extra). Matches `MvpAspirePostgres` locally.  
2. **Production with multiple API replicas or managed SLA:** add **Azure SignalR Service Standard, 1 unit** → budget **≈ €45–50 / month** as the pure incremental cost of realtime vs REST-only; wire the API with `Microsoft.Azure.SignalR`.  
3. Re-price in the calculator before procurement; scale units mainly when **messages** (not connections) approach the per-unit caps for this server-to-server pattern.

**Bottom line:** vs the same Frontend + API + Postgres project **without** SignalR, expect **≈ €0/month** if the hub lives on the API you already run, or **≈ €45–50/month** if you buy one Azure SignalR Standard unit for managed realtime.
