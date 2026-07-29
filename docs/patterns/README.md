# Patterns and Practices — deep dives

`../10-patterns-and-practices.md` is a *catalog*: one paragraph and a source pointer per
pattern, meant to be skimmed by someone who already knows the pattern and just wants to
know where it lives in this codebase. This directory is the opposite: each doc here
explains a pattern from first principles — what problem it solves, what it looks like in
general, a diagram, and then a detailed walk-through of this specific codebase's
implementation with links to the real source. Read `10-patterns-and-practices.md` first if
you just need the map; come here when you want to actually learn the pattern.

This project exists to be learned from (see the root `README.md` — it's derived from a
Greg Young CQRS/Event Sourcing workshop), so these docs assume less background than `00`–`10`
do, and spend more time on *why*, not just *what* and *where*.

## Index

| Doc | Patterns covered |
|---|---|
| `cqrs.md` | CQRS (Command Query Responsibility Segregation) |
| `event-sourcing.md` | Event Sourcing, the Snapshot/Memento pattern |
| `ddd-building-blocks.md` | Aggregate Root, Entity, Value Object, Specification |
| `repository-and-unit-of-work.md` | Repository, Unit of Work, Optimistic Concurrency |
| `messaging-mediator-observer.md` | Mediator, Message Bus, Observer (Rx.NET) |
| `resilience-patterns.md` | Compensating Transaction (Saga-lite) |
| `mvp.md` | Model-View-Presenter (WinForms only) |
| `vue-architecture.md` | The layered (Data/Actions/Structure/Presentation/Styling) component architecture standard for `Fohjin.DDD.WebUI` |
| `winforms-architecture.md` | The layered component architecture standard for `Fohjin.DDD.BankApplication` (WinForms) |
| `wpf-architecture.md` | The MVVM component architecture standard for `Fohjin.DDD.BankApplication.Wpf` |

Each doc ends with a **"See also"** list pointing back at the numbered `00`–`10` docs that
cover the same code from the "what does this system do" angle, and at `10-patterns-and-practices.md`'s
own entry for the same pattern.
