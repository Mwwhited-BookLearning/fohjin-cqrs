namespace Fohjin.DDD.WebApi.OData;

public static class EndpointRouteBuilderExtensions
{
    // .NET 10 has the RFC 10008 QUERY method as a routing primitive (HttpMethods.Query /
    // HttpMethods.IsQuery) but, unlike MapGet/MapPost/etc, no MapQuery convenience yet
    // (docs/supporting/rfc10008-http-query-method.md) - this is that missing helper.
    public static RouteHandlerBuilder MapQuery(this IEndpointRouteBuilder endpoints, string pattern, Delegate handler) =>
        endpoints.MapMethods(pattern, [HttpMethods.Query], handler);
}
