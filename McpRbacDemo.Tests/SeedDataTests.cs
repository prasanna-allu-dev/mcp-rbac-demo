using McpRbacDemo.Data;
using McpRbacDemo.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace McpRbacDemo.Tests;

public class SeedDataTests
{
    [Fact]
    public async Task InitializeAsync_CreatesThreeUsersAndThreeRoles()
    {
        await using var db = TestDbContextFactory.Create();

        await SeedData.InitializeAsync(db);

        Assert.Equal(3, await db.Users.CountAsync());
        Assert.Equal(3, await db.Roles.CountAsync());
        Assert.Equal(3, await db.UserRoles.CountAsync());
    }

    [Fact]
    public async Task InitializeAsync_IsIdempotent()
    {
        var name = Guid.NewGuid().ToString();
        await using var db1 = TestDbContextFactory.Create(name);
        await SeedData.InitializeAsync(db1);

        await using var db2 = TestDbContextFactory.Create(name);
        await SeedData.InitializeAsync(db2);

        Assert.Equal(3, await db2.Users.CountAsync());
    }

    [Fact]
    public async Task InitializeAsync_AssignsCorrectRoles()
    {
        await using var db = TestDbContextFactory.Create();
        await SeedData.InitializeAsync(db);

        var alice = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstAsync(u => u.Username == "alice");

        Assert.Single(alice.UserRoles);
        Assert.Equal("Admin", alice.UserRoles.First().Role.Name);
    }
}
