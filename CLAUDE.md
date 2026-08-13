# Working in this repo

Fohjin.DDD is a CQRS + Event Sourcing reference app. The solution lives under
`Fohjin.DDD.Example/`; the architecture is documented in `docs/00-architecture-overview.md`
through `docs/10-patterns-and-practices.md` — those docs are the living source of truth for
the system's actual shape (there is no separate migration-plan doc anymore; it was removed
once every phase it tracked was done and folded into `00`–`10`). `docs/patterns/` is a
companion set of from-first-principles explanations (with diagrams) of every pattern `10`
catalogs — this project is explicitly meant to be learned from (see the root `README.md`),
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
Vue (`playwright.Chromium.LaunchAsync`, no need for the CDP-attach dance WinForms needs).
Don't assume a plausible-looking change works — prove it against a running system. This has
caught real, otherwise-invisible bugs more than once — every one is narrated in full where it
was fixed, not here: `docs/09-client-uis.md` (four WinForms HTTP-retargeting bugs, the
EventStreamClient JSON-casing bug, the WPF double-click bug, the WinForms bank-card button
bug), `docs/06-event-sourcing-infrastructure.md` (the `AggregateId`-on-creation bug),
`docs/07-messaging-bus.md` (the SSE disconnect exception, the reload-vs-read-model race), and
`docs/00-architecture-overview.md`'s Observability section (the `UseHttpsRedirection()` CORS
gap, Scalar's OAuth2 login gaps).

## Don't invent domain data that doesn't exist

When adding a UI for an existing domain concept (e.g. bank cards), show only what the domain
actually exposes. Check the aggregate/entity's real fields before designing a screen — don't
add a card number, timestamp, or other plausible-sounding field that isn't really there.

## Dev environment

Ports, SQL Server/STS credentials, `Fohjin.DDD.AppHost` behavior, and Observability are all
documented in `docs/00-architecture-overview.md`'s "Dev environment" and "Observability"
sections — read those before assuming a value, don't re-derive or re-guess them.

One thing to actually act on every session, not just know: **Node.js on this machine** —
`C:\repo\oobdev\RunScripts\node.bat`/`npm.bat` sit earlier on `PATH` than the real
`C:\Program Files\nodejs` install and are broken outside an interactive terminal. Prefix any
`npm`/`node` invocation from the Bash tool with `PATH="/c/Program Files/nodejs:$PATH"` —
including before `dotnet run` on `Fohjin.DDD.AppHost` itself, since it spawns the Vue dev
server as a child process that inherits this same broken `PATH` otherwise (the giveaway is
`dcp.exe` listening on 5173 but every request to it timing out).

## Testing

- `dotnet test Fohjin.DDD.sln --filter "FullyQualifiedName!~BankApplication.UI"` — the fast
  unit/integration suite (needs the dev SQL Server instance reachable at port 14330).
- `Fohjin.DDD.BankApplication.UITests` (FlaUI + Playwright) drives the real compiled WinForms
  and WPF `.exe`s end to end (`ClientAndAccountWorkflowTest.cs` /
  `WpfClientAndAccountWorkflowTest.cs`, sharing window-finding helpers in
  `Win32WindowFinder.cs`) — run this after anything touching either desktop client or shared
  backend behavior. Needs STS/WebApi built and a real Edge install. The same project also has
  `VueSignInSignOutTest.cs`, which drives the Vue dev server directly through Playwright
  (headless Chromium, no CDP-attach needed since there's no desktop app in the loop) — its own
  `VueAppFixture.cs` starts `Fohjin.DDD.Sts`, `Fohjin.DDD.WebApi`, and `npm run dev` itself, so
  it needs Node.js on `PATH` in addition to the above (`docs/09-client-uis.md`'s Vue section
  has the bug it guards against).
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
