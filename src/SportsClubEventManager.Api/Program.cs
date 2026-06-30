using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SportsClubEventManager.Api.Middleware;
using SportsClubEventManager.Application;
using SportsClubEventManager.Infrastructure;
using SportsClubEventManager.Infrastructure.Authentication.OAuth2;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddOpenApi();

// Add Application layer services (MediatR, FluentValidation)
builder.Services.AddApplication();

// Add Infrastructure layer services (DbContext, repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// Add Authentication and Authorization
var jwtSecretKey = builder.Configuration["Authentication:JwtSettings:SecretKey"]
    ?? throw new InvalidOperationException("JWT secret key is not configured. Please add it to User Secrets.");

if (jwtSecretKey.Length < 32)
{
    throw new InvalidOperationException(
        $"JWT secret key must be at least 32 characters (256 bits). Current length: {jwtSecretKey.Length}. " +
        "Generate a secure key with: openssl rand -base64 32");
}

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
        ValidIssuer = builder.Configuration["Authentication:JwtSettings:Issuer"] ?? "SportsClubEventManager.Api",
        ValidAudience = builder.Configuration["Authentication:JwtSettings:Audience"] ?? "SportsClubEventManager.Web",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
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
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]
        ?? throw new InvalidOperationException("Google OAuth2 Client ID is not configured.");
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
        ?? throw new InvalidOperationException("Google OAuth2 Client Secret is not configured.");
    options.CallbackPath = builder.Configuration["Authentication:Google:CallbackPath"] ?? "/signin-google";
    options.SaveTokens = true;

    options.Events.OnCreatingTicket = async context =>
    {
        var handler = context.HttpContext.RequestServices.GetRequiredService<GoogleOAuth2Handler>();
        await handler.OnCreatingTicket(context);
    };
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);

builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["https://localhost:5001", "https://localhost:7001"];

        policy.WithOrigins(allowedOrigins)
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
}

app.MapGet("/", () => Results.Ok(new
{
    Name = "Sports Club Event Manager API",
    OpenApi = "/openapi/v1.json"
}));

// Global exception handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// Entry point class for the API application.
/// Made partial and public to allow WebApplicationFactory access in integration tests.
/// </summary>
public partial class Program { }
