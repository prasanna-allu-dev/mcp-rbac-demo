using McpRbacDemo.Data;
using Microsoft.EntityFrameworkCore;

namespace McpRbacDemo.Services;

/// <summary>
/// Resolves a caller's roles from the database and checks them against the
/// configured <see cref="ToolPermissionMap"/>.
/// </summary>
public sealed class RbacService : IRbacService
{
    private readonly AppDbContext _db;
    private readonly ToolPermissionMap _permissions;
    private readonly ICallerIdentityProvider _identity;

    public RbacService(AppDbContext db, ToolPermissionMap permissions, ICallerIdentityProvider identity)
    {
        _db = db;
        _permissions = permissions;
        _identity = identity;
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _db.UserRoles
            .Where(ur => ur.User.Username == username && ur.User.IsActive)
            .Select(ur => ur.Role.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<AuthorizationResult> AuthorizeAsync(
        string? username,
        string toolName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            return AuthorizationResult.NoIdentity(_identity.IdentitySource);

        // Deny by default: a tool with no policy entry is unreachable, so adding a tool
        // without also granting it cannot accidentally expose it.
        if (!_permissions.TryGetAllowedRoles(toolName, out var allowedRoles))
            return AuthorizationResult.UnknownTool(toolName);

        var userRoles = await GetRolesAsync(username, cancellationToken);
        if (userRoles.Count == 0)
            return AuthorizationResult.UnknownPrincipal(toolName);

        return userRoles.Any(allowedRoles.Contains)
            ? AuthorizationResult.Allow()
            : AuthorizationResult.InsufficientRole(toolName);
    }

    public async Task<IReadOnlySet<string>> GetAuthorizedToolsAsync(
        string? username,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // One query for the caller's roles, then a pure in-memory filter over the policy.
        var userRoles = await GetRolesAsync(username, cancellationToken);
        if (userRoles.Count == 0)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var authorized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tool in _permissions.Tools)
        {
            if (_permissions.TryGetAllowedRoles(tool, out var allowedRoles) &&
                userRoles.Any(allowedRoles.Contains))
            {
                authorized.Add(tool);
            }
        }

        return authorized;
    }
}
