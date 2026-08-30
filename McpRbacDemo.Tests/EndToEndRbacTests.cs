using McpRbacDemo.Services;
using McpRbacDemo.Tests.Helpers;

namespace McpRbacDemo.Tests;

/// <summary>
/// Drives the real server with a real MCP client over the real protocol. These are the tests
/// that prove the claim on the tin: what a caller can see, and what a caller can run.
/// </summary>
public class EndToEndRbacTests : IAsyncLifetime
{
    private McpTestServer _server = null!;

    public Task InitializeAsync()
    {
        _server = new McpTestServer();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _server.DisposeAsync();

    [Theory]
    [InlineData("alice", new[] { "audit_log", "generate_report", "inventory_lookup", "manage_users", "system_status" })]
    [InlineData("bob", new[] { "generate_report", "inventory_lookup", "system_status" })]
    [InlineData("carol", new[] { "generate_report", "system_status" })]
    public async Task ToolsList_ShowsOnlyWhatTheCallerMayRun(string user, string[] expected)
    {
        await using var client = await _server.ConnectAsync(user);

        var tools = await client.ListToolsAsync();

        Assert.Equal(expected, tools.Select(t => t.Name).OrderBy(n => n, StringComparer.Ordinal));
    }

    [Fact]
    public async Task ToolsList_IsEmpty_ForAnUnidentifiedCaller()
    {
        await using var client = await _server.ConnectAsync(username: null);

        Assert.Empty(await client.ListToolsAsync());
    }

    [Fact]
    public async Task ToolsList_IsEmpty_ForAnUnknownUser()
    {
        await using var client = await _server.ConnectAsync("mallory");

        Assert.Empty(await client.ListToolsAsync());
    }

    [Fact]
    public async Task ToolsCall_SucceedsForAPermittedTool()
    {
        await using var client = await _server.ConnectAsync("alice");

        var result = await client.CallToolAsync("system_status");

        Assert.False(result.IsError ?? false);
        Assert.Contains("System Status", TextOf(result));
    }

    [Fact]
    public async Task ToolsCall_IsRejectedForAToolTheCallerLacks()
    {
        await using var client = await _server.ConnectAsync("carol");

        var result = await client.CallToolAsync(
            "inventory_lookup",
            new Dictionary<string, object?> { ["query"] = "mouse" });

        Assert.True(result.IsError);
        Assert.Contains("Access denied", TextOf(result));
    }

    [Fact]
    public async Task ToolsCall_IsRejectedEvenWhenTheToolWasHiddenFromDiscovery()
    {
        // Hiding a tool from tools/list is a usability measure, not the boundary.
        // A client that hard-codes the name must still be refused.
        await using var client = await _server.ConnectAsync("carol");
        var visible = (await client.ListToolsAsync()).Select(t => t.Name);
        Assert.DoesNotContain("manage_users", visible);

        var result = await client.CallToolAsync(
            "manage_users",
            new Dictionary<string, object?> { ["action"] = "list" });

        Assert.True(result.IsError);
        Assert.Contains("Access denied", TextOf(result));
    }

    [Fact]
    public async Task ToolsCall_IsRejectedForAnUnidentifiedCaller()
    {
        await using var client = await _server.ConnectAsync(username: null);

        var result = await client.CallToolAsync("system_status");

        Assert.True(result.IsError);
        Assert.Contains("X-User-Id", TextOf(result));
    }

    [Fact]
    public async Task DeniedCall_IsWrittenToTheAuditLog()
    {
        await using (var client = await _server.ConnectAsync("carol"))
        {
            await client.CallToolAsync("manage_users", new Dictionary<string, object?> { ["action"] = "list" });
        }

        var audit = await _server.ReadAuditLogAsync();

        var entry = Assert.Single(audit, a => a.ToolName == "manage_users");
        Assert.Equal("carol", entry.Username);
        Assert.False(entry.Allowed);
        Assert.Equal(AuthorizationOutcome.DeniedInsufficientRole, entry.Outcome);
    }

    [Fact]
    public async Task AllowedCall_IsWrittenToTheAuditLog()
    {
        await using (var client = await _server.ConnectAsync("alice"))
        {
            await client.CallToolAsync("system_status");
        }

        var audit = await _server.ReadAuditLogAsync();

        var entry = Assert.Single(audit, a => a.ToolName == "system_status");
        Assert.Equal("alice", entry.Username);
        Assert.True(entry.Allowed);
        Assert.Equal(AuthorizationOutcome.Allowed, entry.Outcome);
    }

    [Fact]
    public async Task ToolDiscovery_DoesNotPolluteTheAuditLog()
    {
        await using (var client = await _server.ConnectAsync("carol"))
        {
            await client.ListToolsAsync();
        }

        Assert.Empty(await _server.ReadAuditLogAsync());
    }

    private static string TextOf(ModelContextProtocol.Protocol.CallToolResult result) =>
        string.Concat(result.Content.OfType<ModelContextProtocol.Protocol.TextContentBlock>().Select(c => c.Text));
}
