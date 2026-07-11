using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using SportsClubEventManager.Web.HealthChecks;

namespace SportsClubEventManager.Web.Tests.HealthChecks;

/// <summary>
/// Unit tests for the Web host's <see cref="HealthCheckResponseWriter"/> (a deliberate duplicate
/// of the Api host's copy, see design doc "Technology Choices"), verifying the JSON contract it
/// serializes from a <see cref="HealthReport"/> and, in particular, the Development-vs-non-Development
/// branch that truncates non-healthy checks' descriptions to a generic message outside Development
/// to avoid leaking dependency details (issue #41).
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
        var report = CreateReport(("api", new HealthReportEntry(
            HealthStatus.Healthy, "The Api is reachable.", TimeSpan.FromMilliseconds(5), null, null, ["ready"])));

        // Act
        await HealthCheckResponseWriter.WriteAsync(context, report);

        // Assert
        context.Response.ContentType.Should().Be("application/json");
    }

    /// <summary>
    /// Verifies that each check entry serializes its name, status, description, durationMs and
    /// tags in camelCase, matching the same contract as the Api host's copy of this writer.
    /// </summary>
    [Fact]
    public async Task WriteAsync_WhenCalled_SerializesEachCheckEntryInCamelCase()
    {
        // Arrange
        var context = CreateHttpContext(Environments.Development);
        var report = CreateReport(("api", new HealthReportEntry(
            HealthStatus.Healthy, "The Api is reachable.", TimeSpan.FromMilliseconds(5), null, null, ["ready"])));

        // Act
        using var document = await WriteAndParseAsync(context, report);

        // Assert
        var check = document.RootElement.GetProperty("checks")[0];
        check.GetProperty("name").GetString().Should().Be("api");
        check.GetProperty("status").GetString().Should().Be("Healthy");
        check.GetProperty("description").GetString().Should().Be("The Api is reachable.");
        check.TryGetProperty("durationMs", out _).Should().BeTrue();
        check.GetProperty("tags")[0].GetString().Should().Be("ready");
    }

    /// <summary>
    /// Verifies that, in Development, an unhealthy check's real description (e.g. the message
    /// produced by ApiAvailabilityHealthCheck when the Api is unreachable) is preserved as-is.
    /// </summary>
    [Fact]
    public async Task WriteAsync_InDevelopment_PreservesRealDescriptionForUnhealthyCheck()
    {
        // Arrange
        var context = CreateHttpContext(Environments.Development);
        var report = CreateReport(("api", new HealthReportEntry(
            HealthStatus.Unhealthy, "The Api responded with status code 503.", TimeSpan.FromMilliseconds(30), null, null, ["ready"])));

        // Act
        using var document = await WriteAndParseAsync(context, report);

        // Assert
        var description = document.RootElement.GetProperty("checks")[0].GetProperty("description").GetString();
        description.Should().Be("The Api responded with status code 503.");
    }

    /// <summary>
    /// Verifies that outside Development, an unhealthy check's description is replaced with the
    /// generic "A dependency check failed." message, regardless of the real description content.
    /// </summary>
    [Fact]
    public async Task WriteAsync_OutsideDevelopment_TruncatesUnhealthyDescriptionToGenericMessage()
    {
        // Arrange
        var context = CreateHttpContext(Environments.Production);
        var report = CreateReport(("api", new HealthReportEntry(
            HealthStatus.Unhealthy, "The Api could not be reached.", TimeSpan.FromMilliseconds(3000), null, null, ["ready"])));

        // Act
        using var document = await WriteAndParseAsync(context, report);

        // Assert
        var description = document.RootElement.GetProperty("checks")[0].GetProperty("description").GetString();
        description.Should().Be("A dependency check failed.");
    }

    /// <summary>
    /// Verifies that outside Development, a healthy check's description is left untouched.
    /// </summary>
    [Fact]
    public async Task WriteAsync_OutsideDevelopment_PreservesDescriptionForHealthyCheck()
    {
        // Arrange
        var context = CreateHttpContext(Environments.Production);
        var report = CreateReport(("api", new HealthReportEntry(
            HealthStatus.Healthy, "The Api is reachable.", TimeSpan.FromMilliseconds(5), null, null, ["ready"])));

        // Act
        using var document = await WriteAndParseAsync(context, report);

        // Assert
        var description = document.RootElement.GetProperty("checks")[0].GetProperty("description").GetString();
        description.Should().Be("The Api is reachable.");
    }

    /// <summary>
    /// Verifies that the report's overall status, reflecting the worst status among all checks
    /// (e.g. Unhealthy when Web's database check is healthy but its api check is not), is
    /// serialized correctly at the top level. HealthReport computes this aggregate status itself
    /// from the entries passed to its constructor.
    /// </summary>
    [Fact]
    public async Task WriteAsync_WithMixedCheckStatuses_SerializesOverallStatusFromReport()
    {
        // Arrange
        var context = CreateHttpContext(Environments.Development);
        var report = CreateReport(
            ("database", new HealthReportEntry(HealthStatus.Healthy, "Connected.", TimeSpan.FromMilliseconds(5), null, null, ["ready"])),
            ("api", new HealthReportEntry(HealthStatus.Unhealthy, "The Api could not be reached.", TimeSpan.FromMilliseconds(3000), null, null, ["ready"])));

        // Act
        using var document = await WriteAndParseAsync(context, report);

        // Assert
        document.RootElement.GetProperty("status").GetString().Should().Be("Unhealthy");
        document.RootElement.GetProperty("checks").GetArrayLength().Should().Be(2);
    }
}
