using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;
using SportsClubEventManager.Infrastructure;
using SportsClubEventManager.Web.Components;
using SportsClubEventManager.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = builder.Configuration["Authentication:CookieSettings:CookieName"] ?? ".SportsClubEventManager.Auth";
        options.LoginPath = builder.Configuration["Authentication:CookieSettings:LoginPath"] ?? "/login";
        options.LogoutPath = builder.Configuration["Authentication:CookieSettings:LogoutPath"] ?? "/logout";
        options.AccessDeniedPath = builder.Configuration["Authentication:CookieSettings:AccessDeniedPath"] ?? "/access-denied";
        options.ExpireTimeSpan = TimeSpan.Parse(builder.Configuration["Authentication:CookieSettings:ExpireTimeSpan"] ?? "00:30:00");
        options.SlidingExpiration = builder.Configuration.GetValue<bool>("Authentication:CookieSettings:SlidingExpiration", true);
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Add Infrastructure layer services
builder.Services.AddInfrastructure(builder.Configuration);

// Add Radzen services
builder.Services.AddRadzenComponents();

// Add HTTP client for API
builder.Services.AddHttpClient<IEventService, EventService>(client =>
{
    var baseUrl = builder.Configuration["ApiSettings:BaseUrl"]
        ?? throw new InvalidOperationException("ApiSettings:BaseUrl not configured");
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add utility services
builder.Services.AddSingleton<IGuidProvider, GuidProvider>();

var app = builder.Build();

await app.Services.MigrateDatabaseAsync();

// Configure the HTTP request pipeline.
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

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
