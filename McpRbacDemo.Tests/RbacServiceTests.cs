using McpRbacDemo.Services;
using McpRbacDemo.Tests.Helpers;

namespace McpRbacDemo.Tests;

public class RbacServiceTests
{
    [Fact]
    public async Task GetRolesAsync_ReturnsCorrectRole_ForSeededUser()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();
        var svc = new RbacService(db);

        var roles = await svc.GetRolesAsync("alice");

        Assert.Single(roles);
        Assert.Contains("Admin", roles);
    }

    [Fact]
    public async Task GetRolesAsync_ReturnsEmpty_ForUnknownUser()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();
        var svc = new RbacService(db);

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

        var svc = new RbacService(db);
        var roles = await svc.GetRolesAsync("alice");

        Assert.Empty(roles);
    }

    [Theory]
    [InlineData("alice", "inventory_lookup", true)]
    [InlineData("alice", "manage_users", true)]
    [InlineData("alice", "system_status", true)]
    [InlineData("bob", "inventory_lookup", true)]
    [InlineData("bob", "generate_report", true)]
    [InlineData("bob", "manage_users", false)]
    [InlineData("carol", "generate_report", true)]
    [InlineData("carol", "system_status", true)]
    [InlineData("carol", "inventory_lookup", false)]
    [InlineData("carol", "manage_users", false)]
    public async Task IsAuthorizedAsync_EnforcesPermissionMap(string user, string tool, bool expected)
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();
        var svc = new RbacService(db);

        var result = await svc.IsAuthorizedAsync(user, tool);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task IsAuthorizedAsync_ReturnsFalse_ForUnknownTool()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();
        var svc = new RbacService(db);

        var result = await svc.IsAuthorizedAsync("alice", "nonexistent_tool");

        Assert.False(result);
    }

    [Fact]
    public void GetPermissionMap_ReturnsAllFourTools()
    {
        var map = RbacService.GetPermissionMap();

        Assert.Equal(4, map.Count);
        Assert.Contains("inventory_lookup", map.Keys);
        Assert.Contains("generate_report", map.Keys);
        Assert.Contains("manage_users", map.Keys);
        Assert.Contains("system_status", map.Keys);
    }
}
