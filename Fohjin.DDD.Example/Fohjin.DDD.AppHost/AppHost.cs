var builder = DistributedApplication.CreateBuilder(args);

// Docker Compose is generated OUTPUT from this AppHost model (`aspire publish` / `dotnet run --
// --publisher docker-compose`), not a hand-maintained docker-compose.yml - same philosophy as
// the OpenAPI/AsyncAPI docs and NSwag-generated clients elsewhere in this solution
// (docs/supporting/hosting-aspire-docker-compose.md). This is a no-op for the normal
// `dotnet run` (F5) inner-loop; it only matters when publishing.
builder.AddDockerComposeEnvironment("docker-compose");

// One SQL Server instance for every environment (docs/11-migration-plan.md Phase 8 - SQLite
// doesn't containerize as a separate resource the way a real server engine does, so the whole
// solution moved off it). Fixed port/password so the same docs/11-migration-plan.md dev
// conventions (Server=127.0.0.1,14330;...;Password=Dev!Passw0rd) keep working whether the
// database is started by this AppHost or by hand via `docker run` during Phase 8 development.
var sqlPassword = builder.AddParameter("sql-password", "Dev!Passw0rd", secret: true);
var sql = builder.AddSqlServer("sql", sqlPassword, port: 14330)
    .WithDataVolume();

var eventStoreDb = sql.AddDatabase("eventstoredb", "FohjinDomainEventStore");
var reportingDb = sql.AddDatabase("reportingdb", "FohjinReporting");
var stsDb = sql.AddDatabase("stsdb", "FohjinSts");

// Fixed ports (5310/5320/5173) matching every other place in this solution that already hardcodes
// them - STS's own seeded dev-client redirect URIs, the WebApi/Sts CORS policy, the Vue app's
// .env.development, Fohjin.DDD.BankApplication's config, and the FlaUI UI-automation test fixture
// all assume these exact addresses, so this AppHost pins them rather than letting Aspire assign
// random ports the way it would for a resource with no other fixed-address consumers.
var sts = builder.AddProject<Projects.Fohjin_DDD_Sts>("sts")
    .WithHttpEndpoint(port: 5310, name: "http")
    .WithExternalHttpEndpoints()
    .WithReference(stsDb)
    .WaitFor(stsDb);

var webApi = builder.AddProject<Projects.Fohjin_DDD_WebApi>("webapi")
    .WithHttpEndpoint(port: 5320, name: "http")
    .WithExternalHttpEndpoints()
    .WithReference(eventStoreDb)
    .WithReference(reportingDb)
    .WaitFor(eventStoreDb)
    .WaitFor(reportingDb)
    .WaitFor(sts)
    // Deep links to the two API doc endpoints Program.cs already exposes (docs/supporting/
    // asyncapi-saunter.md), surfaced on the resource's own detail page in the Aspire dashboard
    // instead of only being reachable by knowing the path already.
    .WithUrlForEndpoint("http", ep => new() { Url = "/openapi/v1.json", DisplayText = "OpenAPI" })
    .WithUrlForEndpoint("http", ep => new() { Url = "/scalar/v1", DisplayText = "OpenAPI UI (Scalar)" })
    .WithUrlForEndpoint("http", ep => new() { Url = "/asyncapi/asyncapi.json", DisplayText = "AsyncAPI" })
    .WithUrlForEndpoint("http", ep => new() { Url = "/asyncapi/ui/index.html", DisplayText = "AsyncAPI UI" });

// The Vue dev server (Fohjin.DDD.WebUI) - modeled as an Aspire JavaScript/Vite resource so
// `dotnet run` on this AppHost starts it alongside everything else instead of needing a separate
// `npm run dev` in another terminal. Its own .env.development still works standalone for anyone
// running it outside Aspire; these WithEnvironment calls just make the AppHost-orchestrated run
// agree with the same values.
// Browser (OTLP/HTTP, not gRPC - browsers can't do gRPC) telemetry endpoint the dashboard exposes
// alongside its normal OTLP/gRPC one, plus the same API key every project resource already sends
// via OTEL_EXPORTER_OTLP_HEADERS. Both only exist when ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL is
// set on the AppHost process itself (Properties/launchSettings.json) - confirmed empirically by
// running the AppHost with/without it set and diffing the dashboard's own startup banner, which
// only prints an "OTLP/HTTP:" line when that env var is present.
var otlpHttpEndpointUrl = builder.Configuration["ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL"];
var otlpApiKey = builder.Configuration["AppHost:OtlpApiKey"];

var webUi = builder.AddViteApp("webui", "../Fohjin.DDD.WebUI")
    .WithHttpEndpoint(port: 5173, env: "PORT")
    .WithExternalHttpEndpoints()
    .WithEnvironment("VITE_API_BASE_URL", webApi.GetEndpoint("http"))
    .WithEnvironment("VITE_STS_AUTHORITY", "http://127.0.0.1:5310/")
    .WithEnvironment("VITE_STS_CLIENT_ID", "dev-client")
    .WithEnvironment("VITE_OTLP_TRACE_ENDPOINT_URL", otlpHttpEndpointUrl)
    .WithEnvironment("VITE_OTLP_HEADERS", $"x-otlp-api-key={otlpApiKey}")
    .WaitFor(webApi);

builder.Build().Run();
