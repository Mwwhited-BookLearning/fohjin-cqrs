# Working in this repo

Fohjin.DDD is a CQRS + Event Sourcing reference app. The solution lives under
`Fohjin.DDD.Example/`; the architecture is documented in `docs/00-architecture-overview.md`
through `docs/10-patterns-and-practices.md` — those docs are the living source of truth for
the system's actual shape (there is no separate migration-plan doc anymore; it was removed
once every phase it tracked was done and folded into `00`–`10`).

## Keep the docs and the code in sync

Whenever you change the architecture, add a feature, or fix a bug that changes documented
behavior, update the relevant doc(s) in the same piece of work — not as a follow-up someone
might forget. Docs `00`–`10` are read fresh against the actual code when updating them, never
against what an earlier doc/commit *says* happened.

## Verify UI/frontend changes live, not just by type-checking

`vue-tsc`/`dotnet build` passing does not mean a feature works. Start the real stack
(STS + WebApi + Vue dev server, or WinForms) and exercise the feature in an actual browser —
a scratch Playwright script in the scratchpad directory is the established way to do this for
Vue (`playwright.Chromium.LaunchAsync`, no need for the CDP-attach dance WinForms needs). This
caught real, otherwise-invisible bugs more than once:
- Four production bugs in the WinForms HTTP retargeting (missing `Application.Run()`, a
  premature `HttpListener.Stop()`, a DI registration gap, a transient-client bug) — found only
  by driving the compiled `.exe` with FlaUI + Playwright, never by reading the retargeted code.
- A core event-sourcing bug: `BaseAggregateRoot<T>.Apply`/`BaseEntity<T>.Apply` stamped
  `AggregateId` on a domain event *before* dispatching to its handler — but a "created" event's
  handler is what assigns the aggregate its own `Id`, so every aggregate's own creation event
  (`ClientCreatedEvent`, `AccountOpenedEvent`, `ClosedAccountCreatedEvent`) always carried
  `AggregateId = Guid.Empty`. Found because a live Vue client-side event bus filtering on
  `AggregateId` never matched a freshly-created client — every C# unit/integration test passed
  throughout, since none of them observed a live event stream end to end.
- An unordered-child-collection bug that only appeared after the SQLite→SQL Server migration
  (SQLite happened to preserve insertion order; SQL Server doesn't).

Don't assume a plausible-looking change works — prove it against a running system.

## Don't invent domain data that doesn't exist

When adding a UI for an existing domain concept (e.g. bank cards), show only what the domain
actually exposes. Check the aggregate/entity's real fields before designing a screen — don't
add a card number, timestamp, or other plausible-sounding field that isn't really there.

## Dev environment conventions (fixed, not guessed per session)

- Ports: `Fohjin.DDD.Sts` = 5310, `Fohjin.DDD.WebApi` = 5320, Vue dev server = 5173, WinForms
  desktop OIDC loopback = 5330, FlaUI test browser remote-debugging = 9333, SQL Server = 14330.
- SQL Server: one instance for everything (event store, reporting, STS's Identity/OpenIddict
  tables) — `sa` / `Dev!Passw0rd`, `TrustServerCertificate=True;Encrypt=False`. Databases:
  `FohjinDomainEventStore`, `FohjinReporting`, `FohjinSts`. A persistent dev container named
  `fohjin-sqlserver-dev` is the usual way to have one running outside Aspire/AppHost.
- STS seeded dev user: `dev@fohjin.local` / `Dev!Passw0rd`. Seeded client: `dev-client`
  (public, PKCE).
- `Fohjin.DDD.AppHost` (`dotnet run`) boots the whole system — SQL Server container, STS,
  WebApi, Vue dev server — with one command; this is the primary way to run everything
  together, not five separate terminals.
- **Node.js on this machine**: `C:\repo\oobdev\RunScripts\node.bat`/`npm.bat` sit earlier on
  `PATH` than the real `C:\Program Files\nodejs` install and are broken (fail outside an
  interactive terminal). Prefix any `npm`/`node` invocation from the Bash tool with
  `PATH="/c/Program Files/nodejs:$PATH"`.

## Testing

- `dotnet test Fohjin.DDD.sln --filter "FullyQualifiedName!~BankApplication.UI"` — the fast
  unit/integration suite (needs the dev SQL Server instance reachable at port 14330).
- `Test.Fohjin.DDD.BankApplication.UI` (FlaUI + Playwright) drives the real compiled WinForms
  `.exe` end to end — run this after anything touching WinForms or shared backend behavior.
  Needs STS/WebApi built and a real Edge install.
- Both suites passing does not substitute for the live-browser check above when the change is
  UI-facing or touches how a live event stream is consumed.
