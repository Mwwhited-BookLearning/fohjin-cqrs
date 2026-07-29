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

## Sources

- [Get started with NSwag and ASP.NET Core (Microsoft Learn)](https://learn.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-nswag?view=aspnetcore-8.0)
- [NSwag GitHub repo](https://github.com/RicoSuter/NSwag)
- [Generate TypeScript and C# clients with NSwag based on an API (LogRocket)](https://blog.logrocket.com/generate-typescript-c-sharp-clients-nswag-api/)
- [Automatically generating Typescript API clients on build with NSwag](https://blog.sanderaernouts.com/autogenerate-typescript-api-client-with-nswag)
