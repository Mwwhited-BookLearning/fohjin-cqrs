# Working in this repo

Fohjin.DDD is a CQRS + Event Sourcing reference app. The solution lives under
`Fohjin.DDD.Example/`; the architecture is documented in `docs/00-architecture-overview.md`
through `docs/10-patterns-and-practices.md` — those docs are the living source of truth for
the system's actual shape (there is no separate migration-plan doc anymore; it was removed
once every phase it tracked was done and folded into `00`–`10`). `docs/patterns/` is a
companion set of from-first-principles explanations (with diagrams) of every pattern `10`
catalogs — this project is explicitly meant to be learned from (see the root `ReadMe.txt`),
so when a pattern's implementation changes, update both `10`'s catalog entry and its
`patterns/*.md` deep dive, not just one.

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
- `Fohjin.DDD.WebApi`'s leftover `app.UseHttpsRedirection()` (a `dotnet new webapi` template
  default, running before `app.UseCors(...)`) 307-redirected every request — including the
  browser's own CORS preflight `OPTIONS` — to a `launchSettings.json` https port nothing
  listens on under `Fohjin.DDD.AppHost`. Browsers reject any redirect on a preflight outright,
  and even after fixing the ordering, the real request still got redirected cross-origin and
  lost its `Authorization` header doing so — breaking every authenticated call from
  `Fohjin.DDD.WebUI` with an opaque browser-console CORS error and nothing server-side to
  point at it. Only surfaced once something ran the whole stack through
  `Fohjin.DDD.AppHost` end to end in a real browser (see `docs/00-architecture-overview.md`'s
  Observability section) — every prior live-browser check had run the pieces standalone.
- `Fohjin.DDD.WebApi`'s `/api/events` SSE endpoint threw an unhandled `OperationCanceledException`
  on every single client disconnect (browser tab closed, `eventBus.ts` reconnecting) - a
  Visual Studio "first-chance"/user-unhandled exception break, not something visible in the
  Aspire dashboard's own log viewer, which is why a Playwright-driven negative control (comparing
  the same disconnect test against the pre-fix code) showed no difference there even though the
  bug was real - confirmed instead by matching the exact stack trace to `Program.cs`'s `Stream`
  local function. Fixed by catching cancellation there instead of leaving it to escape
  (`docs/07-messaging-bus.md`).
- Getting Scalar's OAuth2 "Authorize" login working against `Fohjin.DDD.Sts` surfaced two more
  CORS/redirect gaps, both only visible by actually clicking the button in a browser: Scalar's
  OAuth2 popup redirects back to its own page rather than a dedicated callback route (needed
  registering as another `dev-client` redirect URI), and the token exchange is a cross-origin
  browser `fetch()` straight to Sts (needed adding WebApi's origin to Sts's CORS policy) - see
  `docs/00-architecture-overview.md`'s Observability section.

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
  `PATH="/c/Program Files/nodejs:$PATH"` — including before `dotnet run` on
  `Fohjin.DDD.AppHost` itself, since it spawns the Vue dev server as a child process that
  inherits this same broken `PATH` otherwise (the Vite resource then silently never starts;
  the giveaway is `dcp.exe` listening on 5173 but every request to it timing out).
- Observability: `Fohjin.DDD.AppHost` reports to its own Aspire dashboard, and that dashboard
  also accepts traces reported directly from the browser (`Fohjin.DDD.WebUI/src/telemetry.ts`)
  over a second OTLP/HTTP endpoint, which only exists because
  `Fohjin.DDD.AppHost/Properties/launchSettings.json` sets
  `ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL` — see `docs/00-architecture-overview.md`'s
  Observability section for why that has to be set there rather than left to Aspire's default.

## Testing

- `dotnet test Fohjin.DDD.sln --filter "FullyQualifiedName!~BankApplication.UI"` — the fast
  unit/integration suite (needs the dev SQL Server instance reachable at port 14330).
- `Test.Fohjin.DDD.BankApplication.UI` (FlaUI + Playwright) drives the real compiled WinForms
  `.exe` end to end — run this after anything touching WinForms or shared backend behavior.
  Needs STS/WebApi built and a real Edge install.
- Both suites passing does not substitute for the live-browser check above when the change is
  UI-facing or touches how a live event stream is consumed.
- `Fohjin.DDD.WebUI` has its own Vitest suite (`npm test` from that directory) covering
  business logic that doesn't need a running browser to verify: `src/events/eventBus.test.ts`
  (the shared SSE connection's retry/backoff/parsing behavior) and
  `src/events/refreshRules.test.ts` (which domain events should make which screen reload -
  extracted out of the views themselves into `src/events/refreshRules.ts` specifically so this
  is testable without mounting a component).
- `Fohjin.DDD.Example/scripts/*.ps1` are the local build/test/coverage pipeline:
  `test-backend.ps1` (`dotnet test` + `reportgenerator` HTML report), `test-frontend.ps1`
  (`npm run test:coverage`), `build.ps1`, and `test-all.ps1` (all three in order, `-OpenReports`
  to open both HTML reports when done). `dotnet tool restore` first if `reportgenerator` isn't
  already restored (`Fohjin.DDD.Example/dotnet-tools.json`).
