# Client generation: NSwag over the generated OpenAPI document

## Capability confirmed

NSwag ([RicoSuter/NSwag](https://github.com/RicoSuter/NSwag)) is current and actively
maintained (docs updated as recently as May 2026 per Microsoft Learn). It combines what
Swashbuckle (OpenAPI generation) and AutoRest (client generation) do separately into one
toolchain, and generates from either a live Swagger/OpenAPI endpoint or a static document:

- A **.NET client** (`NSwag.CodeGeneration.CSharp` / the `nswag` CLI's C# client
  generator) — this is what the retargeted WinForms app will consume instead of calling
  `IBus`/`IDomainRepository`/`IReportingRepository` in-process.
- A **TypeScript client** (`NSwag.CodeGeneration.TypeScript`) — this is what the Vue app
  will consume.

Generation is automatable via the `nswag` CLI (distributed as a .NET tool or an npm
package), driven by a JSON config file, so both clients can be regenerated as part of the
build rather than hand-maintained.

## Design implication

- Generate the OpenAPI document itself using ASP.NET Core 10's built-in
  `Microsoft.AspNetCore.OpenApi` (no need for Swashbuckle as an intermediate — .NET 10
  generates the OpenAPI document natively; NSwag consumes that document as its input).
- Keep the QUERY-verb gap in mind (see `rfc10008-http-query-method.md`) — those endpoints
  won't appear in the generated OpenAPI document as-is, so the generated clients won't have
  methods for them unless that gap is worked around.
- One dedicated project for the generated C# client (e.g. `Fohjin.DDD.ApiClient`) so
  WinForms depends on a small, regenerable project rather than embedding generated code
  directly — mirrors how the EF Core migrations are already treated as regenerated,
  checked-in artifacts elsewhere in this codebase.
- The Vue app's TypeScript client lives inside the Vue project's own source tree,
  regenerated via an npm script wired to the same `nswag` config.

## What was actually implemented

Two things ended up differently than this research originally proposed:

- The Vue TypeScript client is **not** regenerated via its own npm script/`nswag` CLI
  config. `Fohjin.DDD.WebUI` has no Node.js-based NSwag CLI wired up at all — instead,
  `Fohjin.DDD.ApiClient.csproj` has a *second* `OpenApiReference` item
  (`CodeGenerator="NSwagTypeScript"`) that writes straight into
  `Fohjin.DDD.WebUI/src/api/generated-client.ts`, reusing the same
  `NSwag.ApiDescription.Client` MSBuild machinery the C# client already needed. One
  codegen mechanism, two outputs, rather than two separate toolchains.
- Generating `openapi.json` itself is now build-time automated too, closing the one gap
  this doc didn't originally address: `Fohjin.DDD.WebApi.csproj` uses
  `Microsoft.Extensions.ApiDescription.Server` (the build-time counterpart to
  `NSwag.ApiDescription.Client` — same idea Microsoft's own docs describe for
  `Microsoft.AspNetCore.OpenApi`) to generate the document at build time, no running
  server or manual `curl` required, then moves it into
  `Fohjin.DDD.ApiClient/openapi.json`. A `ReferenceOutputAssembly="false"`
  `ProjectReference` from `Fohjin.DDD.ApiClient` to `Fohjin.DDD.WebApi` forces build
  order (WebApi's doc regenerates before ApiClient regenerates both clients from it)
  without creating a real compile-time dependency between the two. See
  `00-architecture-overview.md`.

There's no equivalent gap on the AsyncAPI side (`asyncapi-saunter.md`) - it's served
entirely at runtime by Saunter, with no static snapshot file to ever drift out of sync in
the first place.

## Sources

- [Get started with NSwag and ASP.NET Core (Microsoft Learn)](https://learn.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-nswag?view=aspnetcore-8.0)
- [NSwag GitHub repo](https://github.com/RicoSuter/NSwag)
- [Generate TypeScript and C# clients with NSwag based on an API (LogRocket)](https://blog.logrocket.com/generate-typescript-c-sharp-clients-nswag-api/)
- [Automatically generating Typescript API clients on build with NSwag](https://blog.sanderaernouts.com/autogenerate-typescript-api-client-with-nswag)
