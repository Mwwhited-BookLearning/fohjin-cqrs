using Fohjin.DDD.Reporting;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.OData.Edm;
using System.Text.Json;

namespace Fohjin.DDD.WebApi.OData;

public record ODataQueryRequest(string? Filter);

public static class EndpointRouteBuilderExtensions
{
    // .NET 10 has the RFC 10008 QUERY method as a routing primitive (HttpMethods.Query /
    // HttpMethods.IsQuery) but, unlike MapGet/MapPost/etc, no MapQuery convenience yet
    // (docs/supporting/rfc10008-http-query-method.md) - this is that missing helper.
    public static RouteHandlerBuilder MapQuery(this IEndpointRouteBuilder endpoints, string pattern, Delegate handler) =>
        endpoints.MapMethods(pattern, [HttpMethods.Query], handler);

    // Generalizes the original hand-written /odata/Clients handler (Phase 3) to any reporting
    // DTO: GET and QUERY (RFC 10008, for $filter expressions too large/complex for a query
    // string) share this one delegate, so there is exactly one code path applying OData query
    // options for a given entity set. Backed by IReportingRepository.Query<TDto>() rather than a
    // raw DbContext - the same composable IQueryable every other read path in this app now uses.
    public static void MapODataEntitySet<TDto>(this IEndpointRouteBuilder endpoints, string entitySetName) where TDto : class
    {
        async Task<IResult> Handler(HttpContext httpContext, IReportingRepository repository, [FromKeyedServices("odata")] IEdmModel edmModel)
        {
            if (HttpMethods.IsQuery(httpContext.Request.Method) && httpContext.Request.HasJsonContentType())
            {
                ODataQueryRequest? body;
                try
                {
                    body = await httpContext.Request.ReadFromJsonAsync<ODataQueryRequest>();
                }
                catch (JsonException ex)
                {
                    return Results.BadRequest($"Malformed JSON body: {ex.Message}");
                }

                if (!string.IsNullOrWhiteSpace(body?.Filter))
                    httpContext.Request.QueryString = new QueryString($"?$filter={Uri.EscapeDataString(body.Filter)}");
            }

            var queryOptions = new ODataQueryOptions<TDto>(new ODataQueryContext(edmModel, typeof(TDto), path: null), httpContext.Request);
            try
            {
                queryOptions.Validate(new ODataValidationSettings { AllowedQueryOptions = AllowedQueryOptions.Filter | AllowedQueryOptions.OrderBy });
            }
            catch (ODataException ex)
            {
                return Results.BadRequest(ex.Message);
            }

            var filtered = (IQueryable<TDto>)queryOptions.ApplyTo(repository.Query<TDto>());
            return Results.Ok(await filtered.ToListAsync());
        }

        endpoints.MapGet($"/odata/{entitySetName}", Handler)
            .WithName($"Query{entitySetName}")
            .Produces<IEnumerable<TDto>>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status400BadRequest)
            .RequireAuthorization();

        // Operation id kept as "Query{entitySetName}ViaQueryMethod" - the QUERY-verb OpenAPI
        // document transformer in Program.cs generates its operation under exactly this id, and
        // NSwag turns that into the generated client's method name
        // (Fohjin.DDD.ApiClient.Tests/ODataClientsEndpointTest.cs calls
        // QueryClientsViaQueryMethodAsync end to end).
        endpoints.MapQuery($"/odata/{entitySetName}", Handler)
            .WithName($"Query{entitySetName}ViaQueryMethod")
            .Produces<IEnumerable<TDto>>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status400BadRequest)
            .RequireAuthorization();
    }
}
