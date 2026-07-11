using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Radzen;
using Serilog;
using SportsClubEventManager.Application.Authorization.Policies;
using SportsClubEventManager.Infrastructure;
using SportsClubEventManager.Infrastructure.Configuration;
using SportsClubEventManager.Infrastructure.Logging;
using SportsClubEventManager.Web.Components;
using SportsClubEventManager.Web.Configuration;
using SportsClubEventManager.Web.HealthChecks;
using SportsClubEventManager.Web.Middleware;
using SportsClubEventManager.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddSerilogLogging("SportsClubEventManager.Web");

builder.Configuration.AddDockerSecrets();

// Binds and validates every critical configuration section owned by this host
// (ApiSettings, CookieSettings). Validation runs eagerly during IHost.StartAsync(),
// fixing the previous lazy-validation gap where a missing ApiSettings:BaseUrl let the
// process start successfully and only failed later, on the first typed HttpClient use.
builder.Services.AddWebConfigurationOptions(builder.Configuration);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var cookieSettings = builder.Configuration.GetSection(CookieSettingsOptions.SectionName).Get<CookieSettingsOptions>()
    ?? new CookieSettingsOptions();

// Add Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = cookieSettings.CookieName;
        options.LoginPath = cookieSettings.LoginPath;
        options.LogoutPath = cookieSettings.LogoutPath;
        options.AccessDeniedPath = cookieSettings.AccessDeniedPath;
        options.ExpireTimeSpan = cookieSettings.ExpireTimeSpan;
        options.SlidingExpiration = cookieSettings.SlidingExpiration;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

builder.Services.AddAuthorization(options =>
{
    // Policy requiring authenticated user (any role)
    options.AddPolicy(AuthorizationPolicies.RequireAuthenticatedUser, policy =>
        policy.RequireAuthenticatedUser());

    // Policy requiring Administrator role
    options.AddPolicy(AuthorizationPolicies.RequireAdministratorRole, policy =>
        policy.RequireRole("Administrator"));
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Add Infrastructure layer services
builder.Services.AddInfrastructure(builder.Configuration);

// Add Radzen services
builder.Services.AddRadzenComponents();

// Attaches the signed-in user's Api JWT to every typed HttpClient request below
builder.Services.AddTransient<AuthTokenHandler>();

// One CorrelationId per circuit (DI scope); attaches it to every outgoing Api call and logs
// method/URI/status/elapsed for each of the typed HttpClients registered below.
builder.Services.AddScoped<ICorrelationIdProvider, CorrelationIdProvider>();
builder.Services.AddTransient<CorrelationIdHandler>();
builder.Services.AddTransient<ApiCallLoggingHandler>();

// Single read of ApiSettings:BaseUrl, reused by every typed HttpClient below. The value is
// already guaranteed present and well-formed by AddWebConfigurationOptions()'s
// ValidateOnStart() above, so no further null-check/throw is needed here.
var apiBaseUrl = builder.Configuration.GetSection(ApiSettingsOptions.SectionName).Get<ApiSettingsOptions>()?.BaseUrl
    ?? string.Empty;

// Add HTTP client for API
builder.Services.AddHttpClient<IEventService, EventService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddHttpMessageHandler<AuthTokenHandler>()
    .AddHttpMessageHandler<CorrelationIdHandler>()
    .AddHttpMessageHandler<ApiCallLoggingHandler>();

builder.Services.AddHttpClient<IUserProfileService, UserProfileService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddHttpMessageHandler<AuthTokenHandler>()
    .AddHttpMessageHandler<CorrelationIdHandler>()
    .AddHttpMessageHandler<ApiCallLoggingHandler>();

builder.Services.AddHttpClient<IUserManagementService, UserManagementService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddHttpMessageHandler<AuthTokenHandler>()
    .AddHttpMessageHandler<CorrelationIdHandler>()
    .AddHttpMessageHandler<ApiCallLoggingHandler>();

builder.Services.AddHttpClient<IEventManagementService, EventManagementService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddHttpMessageHandler<AuthTokenHandler>()
    .AddHttpMessageHandler<CorrelationIdHandler>()
    .AddHttpMessageHandler<ApiCallLoggingHandler>();

builder.Services.AddHttpClient<IRegistrationService, RegistrationService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddHttpMessageHandler<AuthTokenHandler>()
    .AddHttpMessageHandler<CorrelationIdHandler>()
    .AddHttpMessageHandler<ApiCallLoggingHandler>();

builder.Services.AddHttpClient<IAdminRegistrationManagementService, AdminRegistrationManagementService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddHttpMessageHandler<AuthTokenHandler>()
    .AddHttpMessageHandler<CorrelationIdHandler>()
    .AddHttpMessageHandler<ApiCallLoggingHandler>();

builder.Services.AddHttpClient<IImportManagementService, ImportManagementService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddHttpMessageHandler<AuthTokenHandler>()
    .AddHttpMessageHandler<CorrelationIdHandler>()
    .AddHttpMessageHandler<ApiCallLoggingHandler>();

// Dedicated HttpClient for the ApiAvailabilityHealthCheck below (issue #41): short timeout so a
// slow Api does not block Web's own health check pipeline, and deliberately no AuthTokenHandler
// since the Api's /health/live probe is anonymous.
builder.Services.AddHttpClient("HealthCheckApiClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(3);
});

// Web's own readiness depends on its database (registered by AddInfrastructure above) and on
// the Api being reachable (its only other runtime dependency).
builder.Services.AddHealthChecks()
    .AddCheck<ApiAvailabilityHealthCheck>("api", tags: ["ready"]);

// Add utility services
builder.Services.AddSingleton<IGuidProvider, GuidProvider>();

var app = builder.Build();

await app.Services.MigrateDatabaseAsync();

// Configure the HTTP request pipeline.

// Correlation id for the initial static render's HTTP request (see CorrelationIdProvider for the
// separate, longer-lived id assigned once the Blazor Server circuit connects).
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.UseAntiforgery();

app.MapGet("/account/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

// Health check endpoints (issue #41). Anonymous by design: orchestrators/monitors cannot
// authenticate. /health/live runs no checks (Predicate = _ => false), the recommended pattern
// for liveness probes, so a transient database/Api blip does not trigger unnecessary restarts;
// /health/ready runs only the checks tagged "ready" (database and Api availability); /health
// runs every registered check. Web has no OpenAPI, so no .WithTags/.WithSummary metadata here.
app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = HealthCheckResponseWriter.WriteAsync
    })
    .AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = HealthCheckResponseWriter.WriteAsync
    })
    .AllowAnonymous();

app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false,
        ResponseWriter = HealthCheckResponseWriter.WriteAsync
    })
    .AllowAnonymous();

app.MapStaticAssets().AllowAnonymous();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
