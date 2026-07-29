using Fohjin.DDD.Bootstrap;
using Fohjin.DDD.Bus;
using Fohjin.DDD.CommandHandlers;
using Fohjin.DDD.Commands;
using Fohjin.DDD.Common;
using Fohjin.DDD.MessageRouting;
using Fohjin.DDD.EventHandlers;
using Fohjin.DDD.EventStore;
using Fohjin.DDD.EventStore.SqlServer;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;
using Fohjin.DDD.Services;
using Fohjin.DDD.WebApi;
using Fohjin.DDD.WebApi.OData;
using Fohjin.DDD.WebApi.Sse;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Saunter;
using Saunter.AsyncApiSchema.v2;
using System.Net.ServerSentEvents;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Declares the same OIDC/OAuth2 authorization-code + PKCE flow Fohjin.DDD.WebUI itself uses
// (docs/supporting/oidc-sts-openiddict-vs-duende.md) as a security scheme in the generated
// OpenAPI document, so Scalar (below) can drive a real "Authorize" login against
// Fohjin.DDD.Sts rather than requiring a token to be pasted in by hand.
//
// Bound eagerly here (rather than resolved via DI) because both the OpenAPI document
// transformer closure and AddJwtBearer's options callback below run while building the service
// collection, before builder.Build() produces a container IOptions<StsOptions> could resolve
// from - Configure<StsOptions>() further down still registers it for any other service that
// wants to inject IOptions<StsOptions> normally.
var stsOptions = builder.Configuration.GetSection(StsOptions.SectionName).Get<StsOptions>() ?? new StsOptions();
builder.Services.Configure<StsOptions>(builder.Configuration.GetSection(StsOptions.SectionName));
var stsAuthority = stsOptions.Authority;
builder.Services.AddOpenApi(options =>
{
    // RFC 10008's QUERY method (docs/supporting/rfc10008-http-query-method.md) is a real,
    // extensible System.Net.Http.HttpMethod value, and Microsoft.OpenApi's own model happily
    // serializes a "query" operation key - confirmed by hand: adding one to
    // OpenApiPathItem.Operations[HttpMethod.Query] round-trips through SerializeAsV31 just like
    // get/post/etc do. The gap is entirely in Microsoft.AspNetCore.OpenApi's endpoint-to-document
    // generator, which silently drops any endpoint mapped only to HttpMethods.Query -
    // /odata/Clients below is mapped to [Get, Query] together, so its "get" operation still gets
    // generated normally; this transformer adds the missing "query" one back in, describing the
    // same filter as a JSON body instead of a query string, so it's visible in Scalar and NSwag
    // generates a real client method for it (confirmed: NSwag emits
    // `request_.Method = new HttpMethod("QUERY")`, a genuine QUERY request, not a POST
    // workaround - Fohjin.DDD.ApiClient.Tests/ODataClientsEndpointTest.cs proves it end to end).
    // This only affects the generated document - the endpoint itself already handles real QUERY
    // requests regardless (see the handler below).
    options.AddDocumentTransformer((document, _, _) =>
    {
        if (document.Paths.TryGetValue("/odata/Clients", out var odataClientsPath) &&
            odataClientsPath?.Operations is { } operations &&
            operations.TryGetValue(System.Net.Http.HttpMethod.Get, out var getOperation) && getOperation is not null &&
            !operations.ContainsKey(System.Net.Http.HttpMethod.Query))
        {
            operations[System.Net.Http.HttpMethod.Query] = new OpenApiOperation
            {
                Tags = getOperation.Tags,
                Summary = "Same as GET /odata/Clients, but for $filter expressions too large/complex for a query string (RFC 10008).",
                OperationId = "QueryClientsViaQueryMethod",
                RequestBody = new OpenApiRequestBody
                {
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>
                                {
                                    ["filter"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null, Description = "An OData $filter expression, e.g. \"contains(Name, 'Smith')\"." },
                                },
                            },
                        },
                    },
                },
                Responses = getOperation.Responses,
            };
        }

        return Task.CompletedTask;
    });
    options.AddDocumentTransformer((document, _, _) =>
    {
        // Served live, MapOpenApi() infers a "servers" entry from the actual incoming request's
        // scheme/host - but Fohjin.DDD.ApiClient/Fohjin.DDD.ApiClient.csproj's build-time document
        // generation (Microsoft.Extensions.ApiDescription.Server) has no HTTP request to infer one
        // from, and the NSwag-generated FohjinApiClient bakes servers[0].url in as its constructor's
        // hardcoded BaseUrl default. Setting it explicitly here keeps both paths identical instead
        // of the generated client silently losing its default the moment doc generation moved from
        // "curl a running server" to "build-time, no server running" (docs/00-architecture-overview.md).
        document.Servers = [new OpenApiServer { Url = "http://127.0.0.1:5320/" }];

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["OAuth2"] = new OpenApiSecurityScheme
        {
            Type = Microsoft.OpenApi.SecuritySchemeType.OAuth2,
            Description = "Client ID: dev-client (public client, PKCE - Fohjin.DDD.Sts's seeded dev client).",
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"{stsAuthority}connect/authorize"),
                    TokenUrl = new Uri($"{stsAuthority}connect/token"),
                    Scopes = new Dictionary<string, string>
                    {
                        ["openid"] = "OpenID",
                        ["profile"] = "Profile",
                        ["email"] = "Email",
                    },
                },
            },
        };
        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("OAuth2", document, null)] = [],
        });
        return Task.CompletedTask;
    });
});

// The Vue SPA (Fohjin.DDD.WebUI) calls this API directly from the browser - needs CORS, unlike
// the WinForms desktop client (Phase 7), which never runs in a browser context at all.
// http://host.docker.internal:5173 is how a Playwright container sees the Vue dev server when
// driving a real headless browser for this project's E2E verification (no native Node.js
// install on this machine - see docs/11-migration-plan.md Phase 6).
const string VueDevCorsPolicy = "VueDev";
builder.Services.AddCors(options => options.AddPolicy(VueDevCorsPolicy, policy => policy
    .WithOrigins("http://localhost:5173", "http://host.docker.internal:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services
    .AddBusServices()
    .AddCommandHandlersServices()
    .AddCommonServices()
    .AddMessageRoutingServices()
    .AddEventHandlersServices()
    .AddEventStoreServices()
    .AddEventStoreSqlServerServices()
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
builder.Services.AddKeyedSingleton<IEdmModel>("odata", edmModel);
builder.Services.AddControllers().AddOData(options => options
    .AddRouteComponents("odata", edmModel)
    .Filter().OrderBy().SetMaxTop(100));

// Phase 4: the SSE event stream gets its own, separate EDM model (over EventEnvelope, not any
// reporting DTO). Both are IEdmModel, so they're registered as KEYED singletons ("odata" /
// "sse") rather than two plain AddSingleton<IEdmModel> calls - the latter would leave the
// service container with two IEdmModel registrations and no way to tell them apart, so
// whichever handler resolves IEdmModel by DI parameter binding gets whichever was registered
// last, silently breaking the other endpoint. Bit us during manual testing before these
// endpoints had their own automated coverage.
builder.Services.AddKeyedSingleton<IEdmModel>("sse", SseEdmModel.Build());

// Saunter documents the /api/events message catalog as AsyncAPI - see
// Fohjin.DDD.WebApi/AsyncApi/DomainEventsAsyncApi.cs and docs/supporting/asyncapi-saunter.md.
builder.Services.AddAsyncApiSchemaGeneration(options =>
{
    options.AssemblyMarkerTypes = [typeof(Program)];
    options.AsyncApi = new AsyncApiDocument
    {
        Info = new Info("Fohjin.DDD domain events", "1.0.0"),
    };
});

// Phase 5: pure standard OIDC discovery against the dev STS (Fohjin.DDD.Sts) - Authority comes
// from configuration and nothing here references any OpenIddict type/package, so swapping in a
// real IdP later (Entra ID, Auth0, Keycloak, ...) is a config change, not a code change
// (docs/supporting/oidc-sts-openiddict-vs-duende.md). Audience validation is deliberately off:
// this API is the STS's only resource, and the dev STS doesn't stamp a matching aud claim
// without extra per-scope resource configuration on its side - validating issuer + signature
// via discovery is enough to reject anything not issued by the configured Authority.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = stsOptions.Authority;
        options.RequireHttpsMetadata = stsOptions.RequireHttpsMetadata;
        options.TokenValidationParameters.ValidateAudience = false;
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapAsyncApiDocuments();
app.MapAsyncApiUi();

await app.Services.BootStrapApplicationAsync();
app.Services.SubscribeEventHandlers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Interactive API explorer reading the /openapi/v1.json document above - the modern
    // replacement for what Swashbuckle's bundled Swagger UI used to provide, since
    // Microsoft.AspNetCore.OpenApi (unlike Swashbuckle.AspNetCore) only generates the raw
    // document and ships no UI of its own. PKCE and the client id are Scalar-UI-specific knobs
    // with no equivalent field in the OpenAPI spec itself (unlike the authorizationUrl/tokenUrl
    // above, which come straight from the document's own OAuth2 flow) - overridden here so
    // Scalar's "Authorize" button drives a working login against Fohjin.DDD.Sts's dev-client,
    // which requires PKCE.
    app.MapScalarApiReference(options => options
        .AddPreferredSecuritySchemes("OAuth2")
        .AddAuthorizationCodeFlow("OAuth2", flow => flow
            .WithClientId("dev-client")
            .WithPkce(Pkce.Sha256)));
}

// No UseHttpsRedirection - this API is deliberately http-only in every dev topology (fixed port
// 5320 with no https counterpart in Fohjin.DDD.AppHost/AppHost.cs, matching Fohjin.DDD.Sts, which
// never had this call). It used to be here as an unremoved `dotnet new webapi` template default,
// redirecting to its own launchSettings.json "https" profile port (7094) even though nothing
// actually listens there under Fohjin.DDD.AppHost. That 307 hit real, hard-to-diagnose traffic:
// the browser's CORS preflight (an OPTIONS request) got redirected and was rejected outright (no
// browser follows a 3xx on a preflight), and even after fixing that by moving CORS first, the
// ACTUAL authenticated request still got redirected cross-origin to :7094 - which strips the
// Authorization header per the fetch redirect spec - breaking every authenticated call from the
// Vue app with an opaque CORS error and no server-side trace of the failure at all. Found live via
// Playwright while verifying browser OTel traces (docs/00-architecture-overview.md) - not
// something the existing test suite could catch, since none of it drives a real cross-origin
// preflight.
app.UseCors(VueDevCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

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
.Produces(StatusCodes.Status202Accepted)
.RequireAuthorization();

app.MapGet("/api/clients", async (IReportingRepository repository) =>
    await repository.GetByExampleAsync<ClientReport>(null))
.WithName("GetClients")
.Produces<IEnumerable<ClientReport>>(StatusCodes.Status200OK)
.RequireAuthorization();

app.MapGet("/api/clients/{id:guid}", async (Guid id, IReportingRepository repository) =>
{
    var client = (await repository.GetByExampleAsync<ClientReport>(new { Id = id })).FirstOrDefault();
    return client is null ? Results.NotFound() : Results.Ok(client);
})
.WithName("GetClientById")
.Produces<ClientReport>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.RequireAuthorization();

// Phase 6: the Client Details screen needs the richer ClientDetailsReport (address, phone
// number, linked accounts) rather than the bare id+name ClientReport above. Same
// IReportingRepository.GetByExampleAsync read pattern as GetClientById - SqlServerReportingRepository
// auto-loads ClientDetailsReport.Accounts/ClosedAccounts via its "{ParentTypeName}Id" convention,
// so no extra join code is needed here.
app.MapGet("/api/clients/{id:guid}/details", async (Guid id, IReportingRepository repository) =>
{
    var client = (await repository.GetByExampleAsync<ClientDetailsReport>(new { Id = id })).FirstOrDefault();
    return client is null ? Results.NotFound() : Results.Ok(client);
})
.WithName("GetClientDetailsById")
.Produces<ClientDetailsReport>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.RequireAuthorization();

// Phase 6: the four edit commands the WinForms Client Details screen uses
// (Fohjin.DDD.BankApplication.Core/Presenters/ClientDetailsPresenter.cs) - same
// bus.Publish(...); bus.CommitAsync(); (fire-and-forget) pattern as CreateClient above.
app.MapPost("/api/clients/{id:guid}/name", (Guid id, ChangeClientNameRequest request, IBus bus) =>
{
    bus.Publish(new ChangeClientNameCommand(id, request.ClientName));
    bus.CommitAsync();
    return Results.Accepted($"/api/clients/{id}/details");
})
.WithName("ChangeClientName")
.Produces(StatusCodes.Status202Accepted)
.RequireAuthorization();

app.MapPost("/api/clients/{id:guid}/address", (Guid id, ClientIsMovingRequest request, IBus bus) =>
{
    bus.Publish(new ClientIsMovingCommand(id, request.Street, request.StreetNumber, request.PostalCode, request.City));
    bus.CommitAsync();
    return Results.Accepted($"/api/clients/{id}/details");
})
.WithName("ChangeClientAddress")
.Produces(StatusCodes.Status202Accepted)
.RequireAuthorization();

app.MapPost("/api/clients/{id:guid}/phone-number", (Guid id, ChangeClientPhoneNumberRequest request, IBus bus) =>
{
    bus.Publish(new ChangeClientPhoneNumberCommand(id, request.PhoneNumber));
    bus.CommitAsync();
    return Results.Accepted($"/api/clients/{id}/details");
})
.WithName("ChangeClientPhoneNumber")
.Produces(StatusCodes.Status202Accepted)
.RequireAuthorization();

app.MapPost("/api/clients/{id:guid}/accounts", (Guid id, OpenNewAccountForClientRequest request, IBus bus) =>
{
    bus.Publish(new OpenNewAccountForClientCommand(id, request.AccountName));
    bus.CommitAsync();
    return Results.Accepted($"/api/clients/{id}/details");
})
.WithName("OpenNewAccountForClient")
.Produces(StatusCodes.Status202Accepted)
.RequireAuthorization();

// Bank cards (docs/02-bank-cards.md): domain + command handlers existed from the original
// CQRS demo, but no UI or endpoint ever called them - GetClientDetailsById above already
// returns ClientDetailsReport.BankCards via SqlServerReportingRepository's "{ParentTypeName}Id"
// convention (BankCardReport.ClientDetailsReportId), so no separate GET endpoint is needed.
app.MapPost("/api/clients/{id:guid}/bank-cards", (Guid id, AssignNewBankCardRequest request, IBus bus) =>
{
    bus.Publish(new AssignNewBankCardCommand(id, request.AccountId));
    bus.CommitAsync();
    return Results.Accepted($"/api/clients/{id}/details");
})
.WithName("AssignNewBankCard")
.Produces(StatusCodes.Status202Accepted)
.RequireAuthorization();

app.MapPost("/api/clients/{id:guid}/bank-cards/{bankCardId:guid}/cancel", (Guid id, Guid bankCardId, IBus bus) =>
{
    bus.Publish(new CancelBankCardCommand(id, bankCardId));
    bus.CommitAsync();
    return Results.Accepted($"/api/clients/{id}/details");
})
.WithName("CancelBankCard")
.Produces(StatusCodes.Status202Accepted)
.RequireAuthorization();

app.MapPost("/api/clients/{id:guid}/bank-cards/{bankCardId:guid}/report-stolen", (Guid id, Guid bankCardId, IBus bus) =>
{
    bus.Publish(new ReportStolenBankCardCommand(id, bankCardId));
    bus.CommitAsync();
    return Results.Accepted($"/api/clients/{id}/details");
})
.WithName("ReportStolenBankCard")
.Produces(StatusCodes.Status202Accepted)
.RequireAuthorization();

// Phase 6: the Account Details screen and its "transfer to" account picker.
app.MapGet("/api/accounts", async (IReportingRepository repository) =>
    await repository.GetByExampleAsync<AccountReport>(null))
.WithName("GetAccounts")
.Produces<IEnumerable<AccountReport>>(StatusCodes.Status200OK)
.RequireAuthorization();

// AccountClosedEventHandler (Fohjin.DDD.EventHandlers) deletes the live AccountDetailsReport
// row and ClosedAccountCreatedEventHandler saves a ClosedAccountDetailsReport in its place
// (same live/closed split as ClientDetailsReport.Accounts/ClosedAccounts) - fall back to the
// closed report so this endpoint still works for an account after it's been closed.
app.MapGet("/api/accounts/{id:guid}/details", async (Guid id, IReportingRepository repository) =>
{
    var account = (await repository.GetByExampleAsync<AccountDetailsReport>(new { Id = id })).FirstOrDefault()
        ?? (await repository.GetByExampleAsync<ClosedAccountDetailsReport>(new { Id = id })).FirstOrDefault();
    return account is null ? Results.NotFound() : Results.Ok(account);
})
.WithName("GetAccountDetailsById")
.Produces<AccountDetailsReport>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.RequireAuthorization();

// Phase 6: the five Account Details edit/transaction commands the WinForms Account Details
// screen uses (Fohjin.DDD.BankApplication.Core/Presenters/AccountDetailsPresenter.cs) - same
// bus.Publish(...); bus.CommitAsync(); (fire-and-forget) pattern as the Client commands above.
app.MapPost("/api/accounts/{id:guid}/name", (Guid id, ChangeAccountNameRequest request, IBus bus) =>
{
    bus.Publish(new ChangeAccountNameCommand(id, request.AccountName));
    bus.CommitAsync();
    return Results.Accepted($"/api/accounts/{id}/details");
})
.WithName("ChangeAccountName")
.Produces(StatusCodes.Status202Accepted)
.RequireAuthorization();

app.MapPost("/api/accounts/{id:guid}/deposit", (Guid id, DepositCashRequest request, IBus bus) =>
{
    bus.Publish(new DepositCashCommand(id, request.Amount));
    bus.CommitAsync();
    return Results.Accepted($"/api/accounts/{id}/details");
})
.WithName("DepositCash")
.Produces(StatusCodes.Status202Accepted)
.RequireAuthorization();

app.MapPost("/api/accounts/{id:guid}/withdrawal", (Guid id, WithdrawalCashRequest request, IBus bus) =>
{
    bus.Publish(new WithdrawalCashCommand(id, request.Amount));
    bus.CommitAsync();
    return Results.Accepted($"/api/accounts/{id}/details");
})
.WithName("WithdrawalCash")
.Produces(StatusCodes.Status202Accepted)
.RequireAuthorization();

app.MapPost("/api/accounts/{id:guid}/transfer", (Guid id, SendMoneyTransferRequest request, IBus bus) =>
{
    bus.Publish(new SendMoneyTransferCommand(id, request.Amount, request.AccountNumber));
    bus.CommitAsync();
    return Results.Accepted($"/api/accounts/{id}/details");
})
.WithName("SendMoneyTransfer")
.Produces(StatusCodes.Status202Accepted)
.RequireAuthorization();

app.MapPost("/api/accounts/{id:guid}/close", (Guid id, IBus bus) =>
{
    bus.Publish(new CloseAccountCommand(id));
    bus.CommitAsync();
    return Results.Accepted($"/api/accounts/{id}/details");
})
.WithName("CloseAccount")
.Produces(StatusCodes.Status202Accepted)
.RequireAuthorization();

// Phase 3: /api/clients above is the plain REST surface from Phase 1; /odata/Clients is the
// new OData one, backed by a real IQueryable rather than IReportingRepository's example-object
// queries. GET and QUERY share this single handler - QUERY (RFC 10008) exists for filters too
// large/complex for a query string, so it takes the same $filter syntax in a JSON body
// instead. Routing them to the same delegate, rather than two independently-written ones, is
// what guarantees identical results for identical filters: there's only one code path applying
// the OData query options.
app.MapMethods("/odata/Clients", [HttpMethods.Get, HttpMethods.Query], async (HttpContext httpContext, IDbContextFactory<ReportingDbContext> dbContextFactory, [FromKeyedServices("odata")] IEdmModel edmModel) =>
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
.Produces<string>(StatusCodes.Status400BadRequest)
.RequireAuthorization();

// Phase 4: live domain events over Server-Sent Events (native System.Net.ServerSentEvents,
// .NET 10). Every event DirectBus.Events (Fohjin.DDD.Bus/Direct/DirectBus.cs) publishes gets
// wrapped in an EventEnvelope and, if it matches the connection's $filter, written to a channel
// that Results.ServerSentEvents streams out. Resolves Phase 4's "full Microsoft.OData.UriParser
// vs. a hand-rolled filter grammar" decision in favor of the former: the same
// ODataQueryOptions/EDM-model machinery Phase 3 built for /odata/Clients is reused here,
// just ApplyTo'd against a one-item queryable per incoming event instead of a DbSet - one
// filter engine for the whole API, not two.
app.MapGet("/api/events", (HttpContext httpContext, IBus bus, [FromKeyedServices("sse")] IEdmModel sseEdmModel) =>
{
    var queryOptions = new ODataQueryOptions<EventEnvelope>(new ODataQueryContext(sseEdmModel, typeof(EventEnvelope), path: null), httpContext.Request);
    try
    {
        queryOptions.Validate(new ODataValidationSettings { AllowedQueryOptions = AllowedQueryOptions.Filter });
    }
    catch (ODataException ex)
    {
        return Results.BadRequest(ex.Message);
    }

    bool Matches(EventEnvelope envelope) =>
        queryOptions.Filter is null || queryOptions.ApplyTo(new[] { envelope }.AsQueryable()).Cast<EventEnvelope>().Any();

    return Results.ServerSentEvents(Stream(bus, Matches, httpContext.RequestAborted));

    static async IAsyncEnumerable<SseItem<EventEnvelope>> Stream(IBus bus, Func<EventEnvelope, bool> matches, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<EventEnvelope>();
        using var subscription = bus.Events
            .Select(EventEnvelope.From)
            .Where(matches)
            .Subscribe(envelope => channel.Writer.TryWrite(envelope));

        // See EventEnvelope.Connected: without this, Results.ServerSentEvents holds the
        // response (headers included) open with zero bytes sent until the first real domain
        // event arrives, which could be never.
        yield return new SseItem<EventEnvelope>(EventEnvelope.Connected, EventEnvelope.Connected.EventType);

        try
        {
            // Not `await foreach (... in channel.Reader.ReadAllAsync(cancellationToken))`: a
            // `yield return` isn't allowed inside a try block that has its own catch (CS1626), and
            // ReadAllAsync's OperationCanceledException needs to be caught right where it's thrown,
            // not left to propagate. WaitToReadAsync/TryRead is the same channel-draining loop
            // without that restriction, since only the await (no yield) sits inside the inner try.
            while (true)
            {
                bool more;
                try
                {
                    more = await channel.Reader.WaitToReadAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // The client (browser tab closed, navigated away, eventBus.ts reconnecting)
                    // went away - httpContext.RequestAborted firing here is the normal, expected
                    // way this stream ends, not a failure. Without this, the exception propagates
                    // straight out of Results.ServerSentEvents' write loop as unhandled, on every
                    // single disconnect.
                    yield break;
                }
                if (!more) yield break;

                while (channel.Reader.TryRead(out var envelope))
                    yield return new SseItem<EventEnvelope>(envelope, envelope.EventType);
            }
        }
        finally
        {
            channel.Writer.TryComplete();
        }
    }
})
.WithName("StreamEvents")
.Produces<string>(StatusCodes.Status400BadRequest)
.RequireAuthorization();

app.Run();

record CreateClientRequest(string? ClientName, string? Street, string? StreetNumber, string? PostalCode, string? City, string? PhoneNumber);
record ChangeClientNameRequest(string? ClientName);
record ClientIsMovingRequest(string? Street, string? StreetNumber, string? PostalCode, string? City);
record ChangeClientPhoneNumberRequest(string? PhoneNumber);
record OpenNewAccountForClientRequest(string? AccountName);
record AssignNewBankCardRequest(Guid AccountId);
record ChangeAccountNameRequest(string? AccountName);
record DepositCashRequest(decimal Amount);
record WithdrawalCashRequest(decimal Amount);
record SendMoneyTransferRequest(decimal Amount, string? AccountNumber);
record ODataQueryRequest(string? Filter);

// Lets WebApplicationFactory<Program> (Fohjin.DDD.ApiClient.Tests) host this app in-process for
// integration tests - top-level statements otherwise leave Program inaccessible to other assemblies.
public partial class Program;
