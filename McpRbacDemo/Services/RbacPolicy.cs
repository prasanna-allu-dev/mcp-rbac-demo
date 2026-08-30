using Microsoft.Extensions.Options;

namespace McpRbacDemo.Services;

/// <summary>
/// The tool-to-role permission policy, bound from the "Rbac" configuration section.
/// Granting a role access to a tool is a configuration change, not a code change.
/// </summary>
public sealed class RbacPolicy
{
    public const string SectionName = "Rbac";

    /// <summary>
    /// Request header carrying the caller's identity. See <see cref="ICallerIdentityProvider"/>
    /// for how to replace header-based identity with real authentication.
    /// </summary>
    public string UserIdHeader { get; set; } = "X-User-Id";

    /// <summary>Maps an MCP tool name to the roles permitted to call it.</summary>
    public Dictionary<string, string[]> ToolPermissions { get; set; } = [];
}

/// <summary>
/// An immutable, case-insensitive view over <see cref="RbacPolicy.ToolPermissions"/>.
/// Built once at startup so per-request authorization is a dictionary lookup.
/// </summary>
public sealed class ToolPermissionMap
{
    private readonly Dictionary<string, IReadOnlySet<string>> _map;

    public ToolPermissionMap(IOptions<RbacPolicy> policy)
    {
        _map = policy.Value.ToolPermissions.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlySet<string>)entry.Value.ToHashSet(StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Every tool named in the policy.</summary>
    public IReadOnlyCollection<string> Tools => _map.Keys;

    /// <summary>
    /// Looks up the roles allowed to call <paramref name="toolName"/>.
    /// A tool absent from the policy yields <c>false</c> — unknown tools are denied by default.
    /// </summary>
    public bool TryGetAllowedRoles(string toolName, out IReadOnlySet<string> allowedRoles) =>
        _map.TryGetValue(toolName, out allowedRoles!);
}

/// <summary>
/// Validates the policy at startup so a misconfigured deployment fails to boot
/// rather than silently denying every call at runtime.
/// </summary>
public sealed class RbacPolicyValidator : IValidateOptions<RbacPolicy>
{
    public ValidateOptionsResult Validate(string? name, RbacPolicy options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.UserIdHeader))
            failures.Add($"{RbacPolicy.SectionName}:{nameof(RbacPolicy.UserIdHeader)} must not be empty.");

        if (options.ToolPermissions.Count == 0)
            failures.Add($"{RbacPolicy.SectionName}:{nameof(RbacPolicy.ToolPermissions)} defines no tools. " +
                         "Every tool would be denied by default.");

        foreach (var (tool, roles) in options.ToolPermissions)
        {
            if (string.IsNullOrWhiteSpace(tool))
                failures.Add("A tool name in the permission policy is empty.");

            // An empty role list is almost always a typo, and denies everyone silently.
            if (roles is null || roles.Length == 0)
                failures.Add($"Tool '{tool}' lists no roles. Remove it, or name the roles that may call it.");
            else if (roles.Any(string.IsNullOrWhiteSpace))
                failures.Add($"Tool '{tool}' has an empty role name.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
