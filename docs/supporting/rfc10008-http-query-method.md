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
- **OpenAPI generation gap (worked around, not a dead end)**: ASP.NET Core 10's OpenAPI
  document generator recognizes the QUERY method exists, but excludes QUERY endpoints from
  the generated OpenAPI document entirely, rather than describing them. The underlying
  `Microsoft.OpenApi` object model has no such gap, though: `OpenApiPathItem.Operations` is
  a plain `Dictionary<HttpMethod, OpenApiOperation>`, and `System.Net.Http.HttpMethod.Query`
  is a real, first-class static member (added specifically for RFC 10008) - confirmed by
  hand that adding an operation keyed on it serializes correctly through
  `SerializeAsV31`. So the gap is entirely in the *generator*, not the *model* or the
  *serializer*: `Fohjin.DDD.WebApi/Program.cs` adds a document transformer that manually
  inserts the missing `"query"` operation for `/odata/Clients` back into the document
  (mirroring its existing GET operation's response shape, describing the filter as a JSON
  request body instead of a query string). NSwag 14.7.1 then generates a real client method
  from it on both sides - `FohjinApiClient.QueryClientsViaQueryMethodAsync(...)` (C#) and
  `queryClientsViaQueryMethod(...)` (TypeScript) - each literally emitting
  `new HttpMethod("QUERY")`/`method: "QUERY"`, not a POST substitute. Verified end to end in
  `Fohjin.DDD.ApiClient.Tests/ODataClientsEndpointTest.cs`.

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
