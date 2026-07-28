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

app.Run();

record CreateClientRequest(string? ClientName, string? Street, string? StreetNumber, string? PostalCode, string? City, string? PhoneNumber);

// Lets WebApplicationFactory<Program> (Test.Fohjin.DDD.ApiClient) host this app in-process for
// integration tests - top-level statements otherwise leave Program inaccessible to other assemblies.
public partial class Program;
