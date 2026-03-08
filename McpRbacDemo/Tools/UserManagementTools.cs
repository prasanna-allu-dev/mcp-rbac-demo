using System.ComponentModel;
using System.Text;
using McpRbacDemo.Data;
using McpRbacDemo.Models;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace McpRbacDemo.Tools;

[McpServerToolType]
public static class UserManagementTools
{
    [McpServerTool(Name = "manage_users"), Description("List users and their roles, or add/remove a user. Pass action='list' to list, action='add' to create a new user, or action='remove' to deactivate one.")]
    public static async Task<string> ManageUsers(
        AppDbContext db,
        [Description("Action to perform: list, add, or remove")] string action,
        [Description("Username (required for add/remove)")] string? username = null,
        [Description("Display name (required for add)")] string? displayName = null,
        [Description("Comma-separated roles to assign (for add)")] string? roles = null)
    {
        switch (action.ToLowerInvariant())
        {
            case "list":
                return await ListUsersAsync(db);

            case "add":
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(displayName))
                    return "Both 'username' and 'displayName' are required when adding a user.";
                return await AddUserAsync(db, username, displayName, roles);

            case "remove":
                if (string.IsNullOrWhiteSpace(username))
                    return "'username' is required when removing a user.";
                return await RemoveUserAsync(db, username);

            default:
                return $"Unknown action '{action}'. Use 'list', 'add', or 'remove'.";
        }
    }

    private static async Task<string> ListUsersAsync(AppDbContext db)
    {
        var users = await db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .OrderBy(u => u.Username)
            .ToListAsync();

        if (users.Count == 0)
            return "No users found.";

        var sb = new StringBuilder();
        sb.AppendLine("Users:");
        foreach (var user in users)
        {
            var roleNames = string.Join(", ", user.UserRoles.Select(ur => ur.Role.Name));
            var status = user.IsActive ? "active" : "inactive";
            sb.AppendLine($"  {user.Username} ({user.DisplayName}) — roles: [{roleNames}] — {status}");
        }
        return sb.ToString();
    }

    private static async Task<string> AddUserAsync(AppDbContext db, string username, string displayName, string? roles)
    {
        if (await db.Users.AnyAsync(u => u.Username == username))
            return $"User '{username}' already exists.";

        var user = new User { Username = username, DisplayName = displayName };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(roles))
        {
            var roleNames = roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var roleName in roleNames)
            {
                var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                if (role != null)
                    db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            }
            await db.SaveChangesAsync();
        }

        return $"User '{username}' created successfully.";
    }

    private static async Task<string> RemoveUserAsync(AppDbContext db, string username)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
            return $"User '{username}' not found.";

        user.IsActive = false;
        await db.SaveChangesAsync();
        return $"User '{username}' has been deactivated.";
    }
}
