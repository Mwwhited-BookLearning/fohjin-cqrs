using Fohjin.DDD.BankApplication;
using Fohjin.DDD.Bus;
using Fohjin.DDD.CommandHandlers;
using Fohjin.DDD.Commands;
using Fohjin.DDD.Common;
using Fohjin.DDD.Configuration;
using Fohjin.DDD.EventHandlers;
using Fohjin.DDD.EventStore;
using Fohjin.DDD.EventStore.SQLite;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;
using Fohjin.DDD.Services;
using Fohjin.DDD.WebApi.OData;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData;
using Microsoft.OData.Edm;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services
    .AddBusServices()
    .AddCommandHandlersServices()
    .AddCommonServices()
    .AddConfigurationServices()
    .AddEventHandlersServices()
    .AddEventStoreServices()
    .AddEventStoreSqliteServices()
    .AddReportingServices()
    .AddDddServices()
    ;

// Phase 3: OData over the reporting DTOs, via the IDbContextFactory<ReportingDbContext>
// AddReportingServices() already registers (used directly below rather than adding a second,
// plain-scoped AddDbContext<ReportingDbContext> registration - EF Core merges the two
// registrations' internal option-configuration services, and since IDbContextFactory<T> is a
// singleton, mixing in a scoped one breaks resolving it from the root provider, which
// SubscribeEventHandlers below does at startup).

// Filter + OrderBy only: the /odata/Clients handler below applies query options by hand
// (ApplyTo + a hard cast back to IQueryable<ClientReport>, then plain System.Text.Json
// serialization) rather than through [EnableQuery]'s own OData-aware formatter, so it can't
// honor $select/$count/$expand - those change the result's shape (a projection wrapper type,
// an envelope with an inline count) that this endpoint doesn't know how to serialize. Enabling
// them here without validating the request would let them through and either throw or be
// silently ignored; ODataValidationSettings.AllowedQueryOptions below (set to just these two)
// is what turns "silently wrong" into a clear 400 instead.
var edmModel = ODataModel.Build();
builder.Services.AddSingleton(edmModel);
builder.Services.AddControllers().AddOData(options => options
    .AddRouteComponents("odata", edmModel)
    .Filter().OrderBy().SetMaxTop(100));

var app = builder.Build();

await app.Services.BootStrapApplicationAsync();
app.Services.SubscribeEventHandlers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Phase 1: prove the wiring - the CQRS core (Bus, CommandHandlers, EventHandlers, EventStore,
// Reporting) is composed exactly as Fohjin.DDD.BankApplication composes it today, just hosted
// over HTTP instead of called in-process from WinForms. No auth, no OData, no SSE yet - those
// arrive in later phases (see docs/11-migration-plan.md).
//
// Note: DirectBus.CommitAsync() is fire-and-forget by design (docs/07-messaging-bus.md) - the
// POST below returns as soon as the command is queued, not once it's been handled. A GET
// immediately afterwards can race the write; this is an existing property of the bus, not
// something Phase 1 introduces.

app.MapPost("/api/clients", (CreateClientRequest request, IBus bus) =>
{
    // CreateClientCommand.Id isn't the persisted client's id: Client.CreateNew (Fohjin.DDD.Domain)
    // assigns the aggregate its own Guid.NewGuid() rather than using the command's, so there's no
    // id to hand back here - callers find the new client via GET /api/clients (pre-existing
    // behavior, not introduced by this endpoint).
    bus.Publish(new CreateClientCommand(Guid.NewGuid(), request.ClientName, request.Street, request.StreetNumber, request.PostalCode, request.City, request.PhoneNumber));
    bus.CommitAsync();
    return Results.Accepted("/api/clients");
})
.WithName("CreateClient")
.Produces(StatusCodes.Status202Accepted);

app.MapGet("/api/clients", async (IReportingRepository repository) =>
    await repository.GetByExampleAsync<ClientReport>(null))
.WithName("GetClients")
.Produces<IEnumerable<ClientReport>>(StatusCodes.Status200OK);

app.MapGet("/api/clients/{id:guid}", async (Guid id, IReportingRepository repository) =>
{
    var client = (await repository.GetByExampleAsync<ClientReport>(new { Id = id })).FirstOrDefault();
    return client is null ? Results.NotFound() : Results.Ok(client);
})
.WithName("GetClientById")
.Produces<ClientReport>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

// Phase 3: /api/clients above is the plain REST surface from Phase 1; /odata/Clients is the
// new OData one, backed by a real IQueryable rather than IReportingRepository's example-object
// queries. GET and QUERY share this single handler - QUERY (RFC 10008) exists for filters too
// large/complex for a query string, so it takes the same $filter syntax in a JSON body
// instead. Routing them to the same delegate, rather than two independently-written ones, is
// what guarantees identical results for identical filters: there's only one code path applying
// the OData query options.
app.MapMethods("/odata/Clients", [HttpMethods.Get, HttpMethods.Query], async (HttpContext httpContext, IDbContextFactory<ReportingDbContext> dbContextFactory, IEdmModel edmModel) =>
{
    if (HttpMethods.IsQuery(httpContext.Request.Method) && httpContext.Request.HasJsonContentType())
    {
        var body = await httpContext.Request.ReadFromJsonAsync<ODataQueryRequest>();
        if (!string.IsNullOrWhiteSpace(body?.Filter))
            httpContext.Request.QueryString = new QueryString($"?$filter={Uri.EscapeDataString(body.Filter)}");
    }

    var queryOptions = new ODataQueryOptions<ClientReport>(new ODataQueryContext(edmModel, typeof(ClientReport), path: null), httpContext.Request);
    try
    {
        queryOptions.Validate(new ODataValidationSettings { AllowedQueryOptions = AllowedQueryOptions.Filter | AllowedQueryOptions.OrderBy });
    }
    catch (ODataException ex)
    {
        return Results.BadRequest(ex.Message);
    }

    await using var context = await dbContextFactory.CreateDbContextAsync();
    var filtered = (IQueryable<ClientReport>)queryOptions.ApplyTo(context.ClientReports);
    return Results.Ok(await filtered.ToListAsync());
})
.WithName("QueryClients")
.Produces<IEnumerable<ClientReport>>(StatusCodes.Status200OK)
.Produces<string>(StatusCodes.Status400BadRequest);

app.Run();

record CreateClientRequest(string? ClientName, string? Street, string? StreetNumber, string? PostalCode, string? City, string? PhoneNumber);
record ODataQueryRequest(string? Filter);

// Lets WebApplicationFactory<Program> (Test.Fohjin.DDD.ApiClient) host this app in-process for
// integration tests - top-level statements otherwise leave Program inaccessible to other assemblies.
public partial class Program;
