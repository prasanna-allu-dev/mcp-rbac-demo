using McpRbacDemo.Data;
using McpRbacDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace McpRbacDemo.Tests.Helpers;

internal static class TestDbContextFactory
{
    public static AppDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    public static async Task<AppDbContext> CreateSeededAsync(string? dbName = null)
    {
        var db = Create(dbName);

        var admin = new Role { Name = "Admin" };
        var manager = new Role { Name = "Manager" };
        var viewer = new Role { Name = "Viewer" };
        db.Roles.AddRange(admin, manager, viewer);

        var alice = new User { Username = "alice", DisplayName = "Alice Johnson" };
        var bob = new User { Username = "bob", DisplayName = "Bob Martinez" };
        var carol = new User { Username = "carol", DisplayName = "Carol Chen" };
        db.Users.AddRange(alice, bob, carol);
        await db.SaveChangesAsync();

        db.UserRoles.AddRange(
            new UserRole { UserId = alice.Id, RoleId = admin.Id },
            new UserRole { UserId = bob.Id, RoleId = manager.Id },
            new UserRole { UserId = carol.Id, RoleId = viewer.Id }
        );
        await db.SaveChangesAsync();

        return db;
    }
}
