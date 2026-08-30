namespace McpRbacDemo.Services;

/// <summary>
/// Answers the two questions the MCP layer needs: may this caller run this tool,
/// and which tools should this caller be shown at all.
/// </summary>
public interface IRbacService
{
    /// <summary>The roles held by an active user. Unknown or deactivated users hold none.</summary>
    Task<IReadOnlyList<string>> GetRolesAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>Decides whether <paramref name="username"/> may call <paramref name="toolName"/>.</summary>
    Task<AuthorizationResult> AuthorizeAsync(string? username, string toolName, CancellationToken cancellationToken = default);

    /// <summary>
    /// The subset of policy-defined tools the caller may invoke. Used to filter tool
    /// discovery so callers never see tools they cannot run.
    /// </summary>
    Task<IReadOnlySet<string>> GetAuthorizedToolsAsync(string? username, CancellationToken cancellationToken = default);
}
