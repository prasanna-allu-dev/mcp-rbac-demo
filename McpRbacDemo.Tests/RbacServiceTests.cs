using McpRbacDemo.Services;
using McpRbacDemo.Tests.Helpers;

namespace McpRbacDemo.Tests;

public class RbacServiceTests
{
    private static RbacService CreateService(McpRbacDemo.Data.AppDbContext db) =>
        new(db, TestPolicy.ShippedMap(), new StubCallerIdentityProvider());

    [Fact]
    public async Task GetRolesAsync_ReturnsCorrectRole_ForSeededUser()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();
        var svc = CreateService(db);

        var roles = await svc.GetRolesAsync("alice");

        Assert.Single(roles);
        Assert.Contains("Admin", roles);
    }

    [Fact]
    public async Task GetRolesAsync_ReturnsEmpty_ForUnknownUser()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();
        var svc = CreateService(db);

        var roles = await svc.GetRolesAsync("nobody");

        Assert.Empty(roles);
    }

    [Fact]
    public async Task GetRolesAsync_ReturnsEmpty_ForInactiveUser()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();
        var alice = db.Users.First(u => u.Username == "alice");
        alice.IsActive = false;
        await db.SaveChangesAsync();

        var svc = CreateService(db);
        var roles = await svc.GetRolesAsync("alice");

        Assert.Empty(roles);
    }

    [Theory]
    [InlineData("alice", "inventory_lookup", true)]
    [InlineData("alice", "generate_report", true)]
    [InlineData("alice", "manage_users", true)]
    [InlineData("alice", "system_status", true)]
    [InlineData("bob", "inventory_lookup", true)]
    [InlineData("bob", "generate_report", true)]
    [InlineData("bob", "manage_users", false)]
    [InlineData("bob", "system_status", true)]
    [InlineData("carol", "inventory_lookup", false)]
    [InlineData("carol", "generate_report", true)]
    [InlineData("carol", "manage_users", false)]
    [InlineData("carol", "system_status", true)]
    [InlineData("alice", "audit_log", true)]
    [InlineData("bob", "audit_log", false)]
    [InlineData("carol", "audit_log", false)]
    public async Task AuthorizeAsync_EnforcesShippedPolicy(string user, string tool, bool expected)
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();
        var svc = CreateService(db);

        var result = await svc.AuthorizeAsync(user, tool);

        Assert.Equal(expected, result.IsAllowed);
    }

    [Fact]
    public async Task AuthorizeAsync_IsCaseInsensitive_ForToolNames()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();
        var svc = CreateService(db);

        var result = await svc.AuthorizeAsync("alice", "INVENTORY_LOOKUP");

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task AuthorizeAsync_DeniesUnknownTool_ByDefault()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();
        var svc = CreateService(db);

        var result = await svc.AuthorizeAsync("alice", "nonexistent_tool");

        Assert.False(result.IsAllowed);
        Assert.Equal(AuthorizationOutcome.DeniedUnknownTool, result.Outcome);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AuthorizeAsync_DeniesMissingIdentity(string? username)
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();
        var svc = CreateService(db);

        var result = await svc.AuthorizeAsync(username, "system_status");

        Assert.False(result.IsAllowed);
        Assert.Equal(AuthorizationOutcome.DeniedNoIdentity, result.Outcome);
    }

    [Fact]
    public async Task AuthorizeAsync_DeniesUnknownPrincipal()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();
        var svc = CreateService(db);

        var result = await svc.AuthorizeAsync("mallory", "system_status");

        Assert.False(result.IsAllowed);
        Assert.Equal(AuthorizationOutcome.DeniedUnknownPrincipal, result.Outcome);
    }

    [Fact]
    public async Task AuthorizeAsync_DeniesDeactivatedUser_WhoPreviouslyHadAccess()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();
        var svc = CreateService(db);
        Assert.True((await svc.AuthorizeAsync("alice", "manage_users")).IsAllowed);

        db.Users.First(u => u.Username == "alice").IsActive = false;
        await db.SaveChangesAsync();

        var result = await svc.AuthorizeAsync("alice", "manage_users");

        Assert.False(result.IsAllowed);
        Assert.Equal(AuthorizationOutcome.DeniedUnknownPrincipal, result.Outcome);
    }

    [Fact]
    public async Task AuthorizeAsync_ReportsInsufficientRole_WhenUserIsKnownButUnprivileged()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();
        var svc = CreateService(db);

        var result = await svc.AuthorizeAsync("carol", "manage_users");

        Assert.Equal(AuthorizationOutcome.DeniedInsufficientRole, result.Outcome);
    }

    [Fact]
    public async Task AuthorizeAsync_DoesNotRevealWhetherAUserExists()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();
        var svc = CreateService(db);

        var unknownUser = await svc.AuthorizeAsync("mallory", "manage_users");
        var knownButDenied = await svc.AuthorizeAsync("carol", "manage_users");

        // Outcomes differ for the audit log, but the caller-facing message must not
        // distinguish "no such user" from "not allowed" — that difference is an oracle.
        Assert.Equal(knownButDenied.Message, unknownUser.Message);
    }

    [Theory]
    [InlineData("alice", new[] { "inventory_lookup", "generate_report", "manage_users", "system_status", "audit_log" })]
    [InlineData("bob", new[] { "inventory_lookup", "generate_report", "system_status" })]
    [InlineData("carol", new[] { "generate_report", "system_status" })]
    public async Task GetAuthorizedToolsAsync_ReturnsOnlyPermittedTools(string user, string[] expected)
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();
        var svc = CreateService(db);

        var tools = await svc.GetAuthorizedToolsAsync(user);

        Assert.Equal(expected.OrderBy(t => t), tools.OrderBy(t => t));
    }

    [Fact]
    public async Task GetAuthorizedToolsAsync_ReturnsNothing_ForAnonymousCaller()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();
        var svc = CreateService(db);

        Assert.Empty(await svc.GetAuthorizedToolsAsync(null));
    }

    [Fact]
    public async Task GetAuthorizedToolsAsync_ReturnsNothing_ForUnknownUser()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();
        var svc = CreateService(db);

        Assert.Empty(await svc.GetAuthorizedToolsAsync("mallory"));
    }
}
