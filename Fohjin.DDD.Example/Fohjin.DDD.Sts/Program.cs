using Fohjin.DDD.Sts.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// The Vue SPA (Fohjin.DDD.WebUI) fetches the OIDC discovery document via a browser XHR before
// the interactive part of the login redirect - that needs CORS, unlike the WinForms desktop
// client (Phase 7), which never runs this code in a browser context at all.
// http://host.docker.internal:5173 is how a Playwright container sees the Vue dev server when
// driving a real headless browser for this project's E2E verification (no native Node.js
// install on this machine - see docs/11-migration-plan.md Phase 6).
// http://127.0.0.1:5320 is Fohjin.DDD.WebApi's own origin - Scalar's OAuth2 "Authorize" flow
// (WebApi/Program.cs's MapScalarApiReference) calls this STS's /connect/token endpoint directly
// from the browser, cross-origin, to exchange the auth code for a token; without it here, that
// call fails client-side with an opaque "Failed to fetch" and no server-side trace at all -
// the same class of live-browser-only CORS gap as the one documented in
// docs/00-architecture-overview.md's Observability section.
const string VueDevCorsPolicy = "VueDev";
builder.Services.AddCors(options => options.AddPolicy(VueDevCorsPolicy, policy => policy
    .WithOrigins("http://localhost:5173", "http://host.docker.internal:5173", "http://127.0.0.1:5320")
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("stsdb")
        ?? throw new NotSupportedException("configuration for ConnectionStrings:stsdb is missing"));

    // Registers the entity sets OpenIddict needs (applications, authorizations, scopes, tokens)
    // into the same DbContext/database as ASP.NET Core Identity's own tables.
    options.UseOpenIddict();
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

builder.Services.AddOpenIddict()
    .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>())
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("connect/authorize")
               .SetEndSessionEndpointUris("connect/logout")
               .SetTokenEndpointUris("connect/token")
               .SetUserInfoEndpointUris("connect/userinfo");

        options.RegisterScopes(Scopes.Email, Scopes.Profile);

        options.AllowAuthorizationCodeFlow();

        // Dev-only certificates, regenerated on every restart - fine for a throwaway dev STS,
        // not something a real deployment would ever use as-is.
        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        // A plain AddJwtBearer(o => o.Authority = ...) resource server (Fohjin.DDD.WebApi) can't
        // decrypt OpenIddict's encrypted-by-default access tokens without also referencing
        // OpenIddict's own validation packages - which is exactly what
        // docs/supporting/oidc-sts-openiddict-vs-duende.md rules out (zero OpenIddict-specific
        // code/types outside this project). Disabling access token encryption keeps the token a
        // plain signed JWT any standard OIDC-aware validator can check via discovery.
        options.DisableAccessTokenEncryption();

        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableEndSessionEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .EnableUserInfoEndpointPassthrough()
               .EnableStatusCodePagesIntegration()
               // Rest of this project runs over plain HTTP in dev (no TLS certs configured
               // anywhere in this solution) - OpenIddict requires HTTPS by default.
               .DisableTransportSecurityRequirement();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

app.MapDefaultEndpoints();

app.UseStaticFiles();
app.UseRouting();

app.UseCors(VueDevCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Migrate the STS's own database (applications/authorizations/scopes/tokens + Identity's user
// table) and seed the one dev client + one dev user this STS exists to serve - matching
// docs/11-migration-plan.md Phase 5's "preconfigured/seeded accounts, no interactive
// registration" decision. Idempotent: safe to run on every startup.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

    // One client for every caller (Phase 5's "one seeded dev client" decision) - a
    // public/PKCE client works the same way for a browser SPA, a desktop loopback redirect, and
    // curl-driven verification, so this just lists all of them. http://127.0.0.1:5310/callback
    // is this STS's own address (Phase 5's curl-driven verification); host.docker.internal is
    // what a Playwright container sees the Vue dev server as when driving a real headless
    // browser through the login flow (Phase 6's verification); http://127.0.0.1:5330/callback/
    // is Fohjin.DDD.BankApplication's desktop loopback listener (Phase 7 - system browser +
    // PKCE, docs/11-migration-plan.md's "desktop OIDC login uses the system browser + loopback
    // redirect" decision); http://127.0.0.1:5320/scalar/v1 is Scalar's own default OAuth2
    // redirect - it redirects back to itself (the page it was opened from) rather than a
    // dedicated callback route, confirmed live (OpenIddict rejects the auth request with
    // invalid_request/"redirect_uri is not valid" otherwise - see WebApi/Program.cs's
    // MapScalarApiReference). Upserted rather than create-once-and-skip, since new redirect URIs
    // get added across phases and a pre-existing seeded application would otherwise never pick
    // them up on an already-migrated dev database.
    var devClientDescriptor = new OpenIddictApplicationDescriptor
    {
        ClientId = "dev-client",
        ClientType = ClientTypes.Public,
        ConsentType = ConsentTypes.Implicit,
        DisplayName = "Fohjin.DDD dev client",
        RedirectUris =
        {
            new Uri("http://127.0.0.1:5310/callback"),
            new Uri("http://localhost:5173/callback"),
            new Uri("http://host.docker.internal:5173/callback"),
            new Uri("http://127.0.0.1:5330/callback/"),
            new Uri("http://127.0.0.1:5320/scalar/v1"),
        },
        Permissions =
        {
            Permissions.Endpoints.Authorization,
            Permissions.Endpoints.Token,
            Permissions.GrantTypes.AuthorizationCode,
            Permissions.ResponseTypes.Code,
            Permissions.Scopes.Email,
            Permissions.Scopes.Profile,
        },
        Requirements = { Requirements.Features.ProofKeyForCodeExchange },
    };

    var existingDevClient = await applicationManager.FindByClientIdAsync("dev-client");
    if (existingDevClient is null)
        await applicationManager.CreateAsync(devClientDescriptor);
    else
        await applicationManager.UpdateAsync(existingDevClient, devClientDescriptor);

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    if (await userManager.FindByNameAsync("dev@fohjin.local") is null)
    {
        var user = new ApplicationUser
        {
            UserName = "dev@fohjin.local",
            Email = "dev@fohjin.local",
            EmailConfirmed = true,
        };
        await userManager.CreateAsync(user, "Dev!Passw0rd");
    }
}

await app.RunAsync();

public partial class Program;
