using McpRbacDemo.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace McpRbacDemo.Tests.Helpers;

/// <summary>
/// Builds policies for tests. <see cref="Shipped"/> reads the real appsettings.json that the
/// app deploys with, so the suite fails if the shipped policy drifts from what the tests assert.
/// </summary>
internal static class TestPolicy
{
    /// <summary>The policy exactly as configured in the application's appsettings.json.</summary>
    public static RbacPolicy Shipped()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var policy = new RbacPolicy();
        configuration.GetSection(RbacPolicy.SectionName).Bind(policy);
        return policy;
    }

    public static ToolPermissionMap ShippedMap() => MapOf(Shipped());

    public static ToolPermissionMap MapOf(RbacPolicy policy) => new(Options.Create(policy));

    /// <summary>A policy built inline, for tests that need a specific shape.</summary>
    public static ToolPermissionMap MapOf(Dictionary<string, string[]> toolPermissions) =>
        MapOf(new RbacPolicy { ToolPermissions = toolPermissions });
}

/// <summary>Returns a fixed caller, standing in for the header/JWT provider in unit tests.</summary>
internal sealed class StubCallerIdentityProvider : ICallerIdentityProvider
{
    public StubCallerIdentityProvider(string? callerName = null) => CallerName = callerName;

    public string? CallerName { get; set; }

    public string IdentitySource => "X-User-Id";

    public string? GetCallerName() => CallerName;
}

/// <summary>Captures audit calls in memory so tests can assert on what was recorded.</summary>
internal sealed class RecordingAuditor : IAuthorizationAuditor
{
    public List<(string? Username, string ToolName, AuthorizationResult Result)> Records { get; } = [];

    public Task RecordAsync(string? username, string toolName, AuthorizationResult result, CancellationToken cancellationToken = default)
    {
        Records.Add((username, toolName, result));
        return Task.CompletedTask;
    }
}
