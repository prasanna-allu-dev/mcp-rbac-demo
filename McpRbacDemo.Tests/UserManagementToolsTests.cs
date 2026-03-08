using McpRbacDemo.Tests.Helpers;
using McpRbacDemo.Tools;
using Microsoft.EntityFrameworkCore;

namespace McpRbacDemo.Tests;

public class UserManagementToolsTests
{
    [Fact]
    public async Task ManageUsers_List_ReturnsAllSeededUsers()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();

        var result = await UserManagementTools.ManageUsers(db, "list");

        Assert.Contains("alice", result);
        Assert.Contains("bob", result);
        Assert.Contains("carol", result);
    }

    [Fact]
    public async Task ManageUsers_Add_CreatesNewUser()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();

        var result = await UserManagementTools.ManageUsers(db, "add", username: "dave", displayName: "Dave Wilson");

        Assert.Contains("created successfully", result);
        Assert.True(await db.Users.AnyAsync(u => u.Username == "dave"));
    }

    [Fact]
    public async Task ManageUsers_Add_AssignsRoles()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();

        await UserManagementTools.ManageUsers(db, "add", username: "eve", displayName: "Eve Park", roles: "Admin,Viewer");

        var eve = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstAsync(u => u.Username == "eve");

        Assert.Equal(2, eve.UserRoles.Count);
    }

    [Fact]
    public async Task ManageUsers_Add_RejectsDuplicate()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();

        var result = await UserManagementTools.ManageUsers(db, "add", username: "alice", displayName: "Alice Again");

        Assert.Contains("already exists", result);
    }

    [Fact]
    public async Task ManageUsers_Add_RequiresUsernameAndDisplayName()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();

        var result = await UserManagementTools.ManageUsers(db, "add");

        Assert.Contains("required", result);
    }

    [Fact]
    public async Task ManageUsers_Remove_DeactivatesUser()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();

        var result = await UserManagementTools.ManageUsers(db, "remove", username: "bob");

        Assert.Contains("deactivated", result);
        var bob = await db.Users.FirstAsync(u => u.Username == "bob");
        Assert.False(bob.IsActive);
    }

    [Fact]
    public async Task ManageUsers_Remove_ReturnsNotFound_ForUnknownUser()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();

        var result = await UserManagementTools.ManageUsers(db, "remove", username: "nobody");

        Assert.Contains("not found", result);
    }

    [Fact]
    public async Task ManageUsers_UnknownAction_ReturnsError()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();

        var result = await UserManagementTools.ManageUsers(db, "delete");

        Assert.Contains("Unknown action", result);
    }
}
