# Documenting SSE event streams with AsyncAPI (Saunter)

## Why AsyncAPI here

OpenAPI describes request/response HTTP operations; it has no vocabulary for a
long-lived, server-pushed stream of many messages over time. AsyncAPI is the equivalent
spec for that shape — channels, messages, publish/subscribe operations — and is the
natural fit for documenting the domain-event SSE stream this project is adding (see
`docs/07-messaging-bus.md` for the existing in-process Rx event fan-out this stream is
built on top of).

## Tooling: Saunter

[Saunter](https://github.com/asyncapi/saunter) is the standard code-first AsyncAPI
generator for .NET — attribute-driven (`[AsyncApi]` on a class, channel/operation
attributes on methods), using NJsonSchema for schema generation, and it hosts the
generated spec document the same way Swashbuckle/NSwag host an OpenAPI document.

It has no SSE-specific transport binding built in — that's fine, because Saunter's job is
describing the *messages and channels*, not the transport. The SSE endpoint itself is
plain ASP.NET Core (.NET 10 ships `System.Net.ServerSentEvents` natively); Saunter just
needs the channel (e.g. `events/{eventType}`) and message schemas (one per domain event
type, reusing the same records already defined in `Fohjin.DDD.Abstractions/Events`)
attributed so it can generate the doc describing what a client will receive.

## Design implication

Treat the AsyncAPI document as describing the **content contract** (which event types
exist, their schemas, the channel naming/filtering convention) rather than trying to model
the OData filter syntax itself inside AsyncAPI — the filter is a query-time concern
(documented in the OpenAPI/QUERY-verb side of the docs), not a message-shape concern.

## Sources

- [Saunter GitHub repo](https://github.com/asyncapi/saunter)
- [AsyncAPI specification — Using Saunter to generate event documentation](https://medium.com/@leisiamedeiros/asyncapi-specification-utilizando-saunter-para-gera%C3%A7%C3%A3o-da-documenta%C3%A7%C3%A3o-31dbb720f2b0)
- [Server-Sent Events in ASP.NET Core and .NET 10 (Milan Jovanović)](https://milanjovanovic.tech/blog/server-sent-events-in-aspnetcore-and-dotnet-10)
- [AsyncAPI Tools directory](https://www.asyncapi.com/tools)
