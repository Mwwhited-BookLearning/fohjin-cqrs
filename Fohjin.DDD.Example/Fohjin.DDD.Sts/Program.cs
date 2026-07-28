using Fohjin.DDD.Sts.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// The Vue SPA (Fohjin.DDD.WebUI) fetches the OIDC discovery document via a browser XHR before
// the interactive part of the login redirect - that needs CORS, unlike the WinForms desktop
// client (Phase 7), which never runs this code in a browser context at all.
// http://host.docker.internal:5173 is how a Playwright container sees the Vue dev server when
// driving a real headless browser for this project's E2E verification (no native Node.js
// install on this machine - see docs/11-migration-plan.md Phase 6).
const string VueDevCorsPolicy = "VueDev";
builder.Services.AddCors(options => options.AddPolicy(VueDevCorsPolicy, policy => policy
    .WithOrigins("http://localhost:5173", "http://host.docker.internal:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(builder.Configuration["Sts:SqliteConnectionString"]
        ?? throw new NotSupportedException("configuration for Sts:SqliteConnectionString is missing"));

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
    if (await applicationManager.FindByClientIdAsync("dev-client") is null)
    {
        await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = "dev-client",
            ClientType = ClientTypes.Public,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = "Fohjin.DDD dev client",
            // One client for both callers (Phase 5's "one seeded dev client" decision) - a
            // public/PKCE client works the same way for a browser SPA and a desktop loopback
            // redirect, so this just lists both. http://127.0.0.1:5310/callback (this STS's
            // own address) is kept for Phase 5's curl-driven verification; Phase 7 will add
            // the WinForms loopback address here too. host.docker.internal is what a
            // Playwright container sees the Vue dev server as when driving a real headless
            // browser through the login flow for Phase 6's verification (docs/11-migration-plan.md).
            RedirectUris =
            {
                new Uri("http://127.0.0.1:5310/callback"),
                new Uri("http://localhost:5173/callback"),
                new Uri("http://host.docker.internal:5173/callback"),
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
        });
    }

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
