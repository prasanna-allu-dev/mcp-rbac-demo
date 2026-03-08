using McpRbacDemo.Data;
using Microsoft.EntityFrameworkCore;

namespace McpRbacDemo.Services;

public class RbacService : IRbacService
{
    private readonly AppDbContext _db;

    // Maps each tool name to the roles that are allowed to call it.
    // Kept here so the policy lives in one place and is easy to extend.
    private static readonly Dictionary<string, HashSet<string>> ToolPermissions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["inventory_lookup"]  = new(StringComparer.OrdinalIgnoreCase) { "Admin", "Manager" },
        ["generate_report"]   = new(StringComparer.OrdinalIgnoreCase) { "Manager", "Viewer" },
        ["manage_users"]      = new(StringComparer.OrdinalIgnoreCase) { "Admin" },
        ["system_status"]     = new(StringComparer.OrdinalIgnoreCase) { "Admin", "Manager", "Viewer" },
    };

    public RbacService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<string>> GetRolesAsync(string username)
    {
        return await _db.UserRoles
            .Where(ur => ur.User.Username == username && ur.User.IsActive)
            .Select(ur => ur.Role.Name)
            .ToListAsync();
    }

    public async Task<bool> IsAuthorizedAsync(string username, string toolName)
    {
        if (!ToolPermissions.TryGetValue(toolName, out var allowedRoles))
            return false;

        var userRoles = await GetRolesAsync(username);
        return userRoles.Any(role => allowedRoles.Contains(role));
    }

    public static IReadOnlyDictionary<string, HashSet<string>> GetPermissionMap() => ToolPermissions;
}
