# RFC 10008 — The HTTP QUERY Method

**Status**: real, current, and very recent. Published **2026-06-15** as a Proposed
Standard on the IETF Standards Track by the HTTPBIS working group (authors: Julian
Reschke, James M. Snell, Mike Bishop). It's the first new standard HTTP method
registered since PATCH in 2010.

## What it defines

`QUERY` is a method that is **safe and idempotent like GET, but carries a request body
like POST**. It closes a long-standing gap: GET technically permits a body but most
tooling/servers don't support or forward it reliably, so anyone needing a complex query
(a large filter expression, a query too big for a URL) has had to abuse POST — which loses
safety/idempotency/cacheability semantics that a real query deserves.

## .NET 10 / ASP.NET Core support (as of this research, 2026-07)

- `Microsoft.AspNetCore.Http.HttpMethods` gained a `Query` constant and an `IsQuery(string)`
  helper.
- **No `MapQuery` convenience method and no `[HttpQuery]` MVC attribute ship in .NET 10** —
  only the primitive constant. Registering an endpoint means using the generic form:
  `app.MapMethods(pattern, [HttpMethods.Query], handler)`. Plan to write a small
  `MapQuery(...)` extension wrapping this for readability, matching the existing
  `MapGet`/`MapPost` style.
- **OpenAPI generation gap**: ASP.NET Core 10's OpenAPI document generator recognizes the
  QUERY method exists, but currently **excludes QUERY endpoints from the generated OpenAPI
  document entirely**, rather than describing them. This directly affects the NSwag
  client-generation plan — see the migration plan's Phase 3 for the workaround (documenting
  the QUERY surface as an equivalent, codegen-friendly POST-with-body operation, or a
  manually-authored OpenAPI fragment merged into the generated doc).

## Design implication for this project

Use `QUERY` for the cases it's actually for — large/complex OData filter expressions that
don't comfortably fit a URL (`$filter` with many clauses, an `$expand` tree, etc.) — while
keeping ordinary `GET` + query-string OData for simple, cacheable, shareable-by-URL queries.
Don't make QUERY the *only* way to query; make it the *escape hatch* for queries too big for
GET, exactly matching the RFC's own stated motivation.

## Sources

- [RFC 10008 - The HTTP QUERY Method](https://datatracker.ietf.org/doc/html/rfc10008)
- [RFC 10008 announcement](https://mailarchive.ietf.org/arch/msg/ietf-announce/uNaYyRDGKjyOn_KDT2JaGLlm9fE/)
- [The New HTTP QUERY Method – and How to Use It Today with .NET 10 (vensas GmbH)](https://vensas.de/en/blog/http-query-method-dotnet-10)
- [Adding Experimental HTTP Methods To ASP.NET Core (Khalid Abuhakmeh)](https://khalidabuhakmeh.com/adding-experimental-http-methods-to-aspnet-core)
- [HttpQuery sample repo (khalidabuhakmeh)](https://github.com/khalidabuhakmeh/HttpQuery)
