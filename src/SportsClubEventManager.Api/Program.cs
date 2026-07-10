using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using SportsClubEventManager.Api.Configuration;
using SportsClubEventManager.Api.Middleware;
using SportsClubEventManager.Application;
using SportsClubEventManager.Application.Authorization.Policies;
using SportsClubEventManager.Infrastructure;
using SportsClubEventManager.Infrastructure.Authentication.OAuth2;
using SportsClubEventManager.Infrastructure.Configuration;
using SportsClubEventManager.Infrastructure.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddSerilogLogging("SportsClubEventManager.Api");

builder.Configuration.AddDockerSecrets();

// Binds and validates every critical configuration section owned by this host
// (JwtSettings, Google, AdminUser, Cors). Validation runs eagerly during
// IHost.StartAsync(), aggregating every failure into a single exception raised
// before the process accepts any HTTP request.
builder.Services.AddApiConfigurationOptions(builder.Configuration);

// Add services to the container
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddOpenApi();

// Add Application layer services (MediatR, FluentValidation)
builder.Services.AddApplication();

// Add Infrastructure layer services (DbContext, repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// Add Authentication and Authorization
// AddJwtBearer/AddGoogle need concrete values at builder-construction time (not a deferred
// IOptions<T>), so the same sections are read directly here a second time; the
// ValidateOnStart() registered above remains the source of the "fails fast at startup if
// something is missing" guarantee.
var jwtSettings = builder.Configuration.GetSection(JwtSettingsOptions.SectionName).Get<JwtSettingsOptions>()
    ?? new JwtSettingsOptions();
var googleAuth = builder.Configuration.GetSection(GoogleAuthOptions.SectionName).Get<GoogleAuthOptions>()
    ?? new GoogleAuthOptions();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.ContainsKey("access_token"))
            {
                context.Token = context.Request.Cookies["access_token"];
            }
            return Task.CompletedTask;
        }
    };
})
.AddGoogle(options =>
{
    options.ClientId = googleAuth.ClientId;
    options.ClientSecret = googleAuth.ClientSecret;
    options.CallbackPath = googleAuth.CallbackPath;
    options.SaveTokens = true;

    options.Events.OnCreatingTicket = async context =>
    {
        var handler = context.HttpContext.RequestServices.GetRequiredService<GoogleOAuth2Handler>();
        await handler.OnCreatingTicket(context);
    };
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);

builder.Services.AddAuthorization(options =>
{
    // Default policy: require authenticated user
    options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    // Policy requiring authenticated user (any role)
    options.AddPolicy(AuthorizationPolicies.RequireAuthenticatedUser, policy =>
        policy.RequireAuthenticatedUser());

    // Policy requiring Administrator role
    options.AddPolicy(AuthorizationPolicies.RequireAdministratorRole, policy =>
        policy.RequireRole("Administrator"));
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()
            ?? new CorsOptions();

        policy.WithOrigins(corsOptions.AllowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

await app.Services.MigrateDatabaseAsync();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Sports Club Event Manager API v1");
        options.RoutePrefix = "swagger";
    });
}

app.MapGet("/", () => Results.Ok(new
{
    Name = "Sports Club Event Manager API",
    OpenApi = "/openapi/v1.json",
    Swagger = "/swagger"
}));

// Correlation id: reads/generates X-Correlation-Id and pushes it into the ambient LogContext
// for the rest of the request, before anything else (including the request logging below) runs.
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseSerilogRequestLogging(options =>
{
    // Verbose (not Information) for /health*, so periodic Docker/orchestrator probes (issue #41)
    // don't flood the logs with a line every few seconds; they still show up if the minimum
    // level is raised.
    options.GetLevel = (httpContext, elapsed, ex) =>
        httpContext.Request.Path.StartsWithSegments("/health") ? LogEventLevel.Verbose : LogEventLevel.Information;
});

// Global exception handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();

// Pushes UserId/UserRole into the ambient LogContext (placed after UseAuthorization to access
// populated User claims), so they're attached to every remaining log line for this request.
app.UseMiddleware<RequestUserLogContextMiddleware>();

// Unauthorized access logging (placed after UseAuthorization to access populated User claims)
app.UseMiddleware<UnauthorizedAccessLoggingMiddleware>();

app.MapControllers();

app.Run();

/// <summary>
/// Entry point class for the API application.
/// Made partial and public to allow WebApplicationFactory access in integration tests.
/// </summary>
public partial class Program { }
