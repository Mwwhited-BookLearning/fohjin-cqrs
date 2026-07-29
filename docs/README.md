# Fohjin.DDD documentation

Start here. This is the living source of truth for the system's actual shape — read fresh
against the code, not against what an earlier doc or commit says happened (see
`../CLAUDE.md`'s "Keep the docs and the code in sync" for the standing rule behind that).

## System overview

| Doc | Covers |
|---|---|
| [`00-architecture-overview.md`](00-architecture-overview.md) | The system-level (C4 Context/Container) view: every piece, how they connect, the request lifecycle, and Observability |

## Domain, feature by feature

| Doc | Covers |
|---|---|
| [`01-client-management.md`](01-client-management.md) | `Client` aggregate: create, rename, move, change phone number |
| [`02-bank-cards.md`](02-bank-cards.md) | `BankCard` child entity: assign, cancel, report stolen |
| [`03-account-management.md`](03-account-management.md) | `ActiveAccount`/`ClosedAccount`: open, close, rename |
| [`04-cash-operations.md`](04-cash-operations.md) | Deposit and withdrawal |
| [`05-money-transfers.md`](05-money-transfers.md) | Internal/external transfer routing and compensating failure |

## Infrastructure

| Doc | Covers |
|---|---|
| [`06-event-sourcing-infrastructure.md`](06-event-sourcing-infrastructure.md) | Aggregate roots, the event store, snapshots |
| [`07-messaging-bus.md`](07-messaging-bus.md) | Command dispatch (Mediator) and event fan-out (Observer/Rx) |
| [`08-reporting-read-models.md`](08-reporting-read-models.md) | Read-model DTOs and their event-driven updates |
| [`09-winforms-ui.md`](09-winforms-ui.md) | Both UI clients: WinForms Presenter/View and Vue as a sibling client |

## Patterns and practices

| Doc | Covers |
|---|---|
| [`10-patterns-and-practices.md`](10-patterns-and-practices.md) | Concise catalog: every named pattern used, one paragraph + source pointer each |
| [`patterns/`](patterns/README.md) | The same patterns explained from first principles, with diagrams — for learning a pattern, not just locating it |

## Why decisions were made this way

| Doc | Covers |
|---|---|
| [`supporting/`](supporting/README.md) | Research written at the point each technology choice was made (OpenIddict vs. Duende, OData vs. hand-rolled filtering, Aspire/Docker Compose, NSwag, ...) |
