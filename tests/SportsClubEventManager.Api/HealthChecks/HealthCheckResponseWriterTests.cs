using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using SportsClubEventManager.Api.HealthChecks;
using Xunit;

namespace SportsClubEventManager.Api.Tests.HealthChecks;

/// <summary>
/// Unit tests for <see cref="HealthCheckResponseWriter"/>, verifying the JSON contract it
/// serializes from a <see cref="HealthReport"/> and, in particular, the Development-vs-non-Development
/// branch that truncates unhealthy checks' descriptions to a generic message outside Development
/// to avoid leaking dependency details (issue #41) — a branch that is hard to exercise reliably
/// through a full WebApplicationFactory-driven integration test, since that would require forcing
/// a real unhealthy dependency while also controlling ASPNETCORE_ENVIRONMENT per test case.
/// </summary>
public sealed class HealthCheckResponseWriterTests
{
    private static HttpContext CreateHttpContext(string environmentName)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);

        var services = new ServiceCollection();
        services.AddSingleton(environment);

        var context = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            Response = { Body = new MemoryStream() }
        };

        return context;
    }

    private static HealthReport CreateReport(params (string Name, HealthReportEntry Entry)[] entries)
    {
        var dictionary = entries.ToDictionary(e => e.Name, e => e.Entry);
        return new HealthReport(dictionary, TimeSpan.FromMilliseconds(12.5));
    }

    private static async Task<JsonDocument> WriteAndParseAsync(HttpContext context, HealthReport report)
    {
        await HealthCheckResponseWriter.WriteAsync(context, report);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();

        return JsonDocument.Parse(json);
    }

    /// <summary>
    /// Verifies that the response Content-Type is set to application/json.
    /// </summary>
    [Fact]
    public async Task WriteAsync_WhenCalled_SetsJsonContentType()
    {
        // Arrange
        var context = CreateHttpContext(Environments.Development);
        var report = CreateReport(("database", new HealthReportEntry(
            HealthStatus.Healthy, "Connected.", TimeSpan.FromMilliseconds(5), null, null, ["ready"])));

        // Act
        await HealthCheckResponseWriter.WriteAsync(context, report);

        // Assert
        context.Response.ContentType.Should().Be("application/json");
    }

    /// <summary>
    /// Verifies that the top-level JSON shape uses camelCase property names for status,
    /// totalDurationMs and checks, matching the documented contract.
    /// </summary>
    [Fact]
    public async Task WriteAsync_WhenCalled_SerializesTopLevelShapeInCamelCase()
    {
        // Arrange
        var context = CreateHttpContext(Environments.Development);
        var report = CreateReport(("database", new HealthReportEntry(
            HealthStatus.Healthy, "Connected.", TimeSpan.FromMilliseconds(5), null, null, ["ready"])));

        // Act
        using var document = await WriteAndParseAsync(context, report);

        // Assert
        var root = document.RootElement;
        root.TryGetProperty("status", out var status).Should().BeTrue();
        status.GetString().Should().Be("Healthy");
        root.TryGetProperty("totalDurationMs", out _).Should().BeTrue();
        root.TryGetProperty("checks", out var checks).Should().BeTrue();
        checks.ValueKind.Should().Be(JsonValueKind.Array);
    }

    /// <summary>
    /// Verifies that each check entry serializes its name, status, description, durationMs and
    /// tags in camelCase.
    /// </summary>
    [Fact]
    public async Task WriteAsync_WhenCalled_SerializesEachCheckEntryInCamelCase()
    {
        // Arrange
        var context = CreateHttpContext(Environments.Development);
        var report = CreateReport(("database", new HealthReportEntry(
            HealthStatus.Healthy, "Connected.", TimeSpan.FromMilliseconds(5), null, null, ["ready"])));

        // Act
        using var document = await WriteAndParseAsync(context, report);

        // Assert
        var check = document.RootElement.GetProperty("checks")[0];
        check.GetProperty("name").GetString().Should().Be("database");
        check.GetProperty("status").GetString().Should().Be("Healthy");
        check.GetProperty("description").GetString().Should().Be("Connected.");
        check.TryGetProperty("durationMs", out _).Should().BeTrue();
        check.GetProperty("tags")[0].GetString().Should().Be("ready");
    }

    /// <summary>
    /// Verifies that, in Development, an unhealthy check's real description is preserved as-is
    /// (no truncation), so developers can see the actual failure detail locally.
    /// </summary>
    [Fact]
    public async Task WriteAsync_InDevelopment_PreservesRealDescriptionForUnhealthyCheck()
    {
        // Arrange
        var context = CreateHttpContext(Environments.Development);
        var report = CreateReport(("database", new HealthReportEntry(
            HealthStatus.Unhealthy,
            "A network-related or instance-specific error occurred while establishing a connection to SQL Server sql-prod-01.",
            TimeSpan.FromMilliseconds(30),
            null,
            null,
            ["ready"])));

        // Act
        using var document = await WriteAndParseAsync(context, report);

        // Assert
        var description = document.RootElement.GetProperty("checks")[0].GetProperty("description").GetString();
        description.Should().Contain("sql-prod-01");
    }

    /// <summary>
    /// Verifies that, in Development, when a check has no explicit description but does have an
    /// attached exception, the exception's message is used as the description.
    /// </summary>
    [Fact]
    public async Task WriteAsync_InDevelopmentWithNoDescriptionButWithException_UsesExceptionMessage()
    {
        // Arrange
        var context = CreateHttpContext(Environments.Development);
        var exception = new InvalidOperationException("Could not connect to the database.");
        var report = CreateReport(("database", new HealthReportEntry(
            HealthStatus.Unhealthy, null, TimeSpan.FromMilliseconds(30), exception, null, ["ready"])));

        // Act
        using var document = await WriteAndParseAsync(context, report);

        // Assert
        var description = document.RootElement.GetProperty("checks")[0].GetProperty("description").GetString();
        description.Should().Be("Could not connect to the database.");
    }

    /// <summary>
    /// Verifies that outside Development (e.g. Production), an unhealthy check's description is
    /// replaced with the generic "A dependency check failed." message, regardless of what the
    /// real description or exception message contained, to avoid leaking dependency details such
    /// as server names.
    /// </summary>
    [Fact]
    public async Task WriteAsync_OutsideDevelopment_TruncatesUnhealthyDescriptionToGenericMessage()
    {
        // Arrange
        var context = CreateHttpContext(Environments.Production);
        var report = CreateReport(("database", new HealthReportEntry(
            HealthStatus.Unhealthy,
            "A network-related or instance-specific error occurred while establishing a connection to SQL Server sql-prod-01.",
            TimeSpan.FromMilliseconds(30),
            null,
            null,
            ["ready"])));

        // Act
        using var document = await WriteAndParseAsync(context, report);

        // Assert
        var description = document.RootElement.GetProperty("checks")[0].GetProperty("description").GetString();
        description.Should().Be("A dependency check failed.");
    }

    /// <summary>
    /// Verifies that outside Development, a healthy check's description is left untouched — the
    /// truncation only applies to non-healthy checks, since a healthy description carries no
    /// sensitive failure detail.
    /// </summary>
    [Fact]
    public async Task WriteAsync_OutsideDevelopment_PreservesDescriptionForHealthyCheck()
    {
        // Arrange
        var context = CreateHttpContext(Environments.Production);
        var report = CreateReport(("database", new HealthReportEntry(
            HealthStatus.Healthy, "Connected.", TimeSpan.FromMilliseconds(5), null, null, ["ready"])));

        // Act
        using var document = await WriteAndParseAsync(context, report);

        // Assert
        var description = document.RootElement.GetProperty("checks")[0].GetProperty("description").GetString();
        description.Should().Be("Connected.");
    }

    /// <summary>
    /// Verifies that outside Development, a Degraded check's description is also truncated to the
    /// generic message, since the truncation rule is "anything other than Healthy", not just
    /// Unhealthy.
    /// </summary>
    [Fact]
    public async Task WriteAsync_OutsideDevelopmentWithDegradedCheck_TruncatesDescriptionToGenericMessage()
    {
        // Arrange
        var context = CreateHttpContext(Environments.Staging);
        var report = CreateReport(("api", new HealthReportEntry(
            HealthStatus.Degraded, "Slow response from internal host db-internal-02.", TimeSpan.FromMilliseconds(2900), null, null, ["ready"])));

        // Act
        using var document = await WriteAndParseAsync(context, report);

        // Assert
        var description = document.RootElement.GetProperty("checks")[0].GetProperty("description").GetString();
        description.Should().Be("A dependency check failed.");
    }

    /// <summary>
    /// Verifies that a report with multiple check entries serializes all of them, not just the
    /// first one.
    /// </summary>
    [Fact]
    public async Task WriteAsync_WithMultipleChecks_SerializesAllEntries()
    {
        // Arrange
        var context = CreateHttpContext(Environments.Development);
        var report = CreateReport(
            ("database", new HealthReportEntry(HealthStatus.Healthy, "Connected.", TimeSpan.FromMilliseconds(5), null, null, ["ready"])),
            ("api", new HealthReportEntry(HealthStatus.Healthy, "The Api is reachable.", TimeSpan.FromMilliseconds(20), null, null, ["ready"])));

        // Act
        using var document = await WriteAndParseAsync(context, report);

        // Assert
        document.RootElement.GetProperty("checks").GetArrayLength().Should().Be(2);
    }
}
