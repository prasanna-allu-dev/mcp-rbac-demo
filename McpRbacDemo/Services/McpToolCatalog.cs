using System.Reflection;
using ModelContextProtocol.Server;

namespace McpRbacDemo.Services;

/// <summary>
/// The tools this assembly actually declares, discovered the same way the MCP SDK discovers
/// them — by reflecting over <see cref="McpServerToolTypeAttribute"/> and
/// <see cref="McpServerToolAttribute"/>.
/// <para>
/// Having the code's view of "what tools exist" available at runtime lets startup verify it
/// against the configured policy, so a tool that was added but never granted is caught at boot
/// instead of failing the first time somebody calls it.
/// </para>
/// </summary>
public static class McpToolCatalog
{
    public static IReadOnlyCollection<string> DeclaredToolNames { get; } = Discover();

    private static string[] Discover()
    {
        return typeof(McpToolCatalog).Assembly
            .GetTypes()
            .Where(type => type.GetCustomAttribute<McpServerToolTypeAttribute>() is not null)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Select(method => method.GetCustomAttribute<McpServerToolAttribute>())
            .Where(attribute => attribute is not null && !string.IsNullOrWhiteSpace(attribute.Name))
            .Select(attribute => attribute!.Name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Tools declared in code that the policy never mentions. Each one is unreachable,
    /// because unknown tools are denied by default.
    /// </summary>
    public static IReadOnlyList<string> FindToolsMissingFromPolicy(ToolPermissionMap permissions) =>
        DeclaredToolNames
            .Where(tool => !permissions.TryGetAllowedRoles(tool, out _))
            .OrderBy(tool => tool, StringComparer.Ordinal)
            .ToList();
}
