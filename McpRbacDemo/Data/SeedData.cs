using McpRbacDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace McpRbacDemo.Data;

public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (await db.Users.AnyAsync())
            return;

        var adminRole = new Role { Name = "Admin" };
        var managerRole = new Role { Name = "Manager" };
        var viewerRole = new Role { Name = "Viewer" };

        db.Roles.AddRange(adminRole, managerRole, viewerRole);

        var alice = new User { Username = "alice", DisplayName = "Alice Johnson" };
        var bob = new User { Username = "bob", DisplayName = "Bob Martinez" };
        var carol = new User { Username = "carol", DisplayName = "Carol Chen" };

        db.Users.AddRange(alice, bob, carol);
        await db.SaveChangesAsync();

        db.UserRoles.AddRange(
            new UserRole { UserId = alice.Id, RoleId = adminRole.Id },
            new UserRole { UserId = bob.Id, RoleId = managerRole.Id },
            new UserRole { UserId = carol.Id, RoleId = viewerRole.Id }
        );

        await db.SaveChangesAsync();
    }
}
