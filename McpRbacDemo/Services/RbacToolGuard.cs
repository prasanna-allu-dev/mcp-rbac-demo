namespace McpRbacDemo.Services;

/// <summary>
/// The single place where identity, policy, and audit meet. The MCP filters stay thin
/// delegates over this service, which keeps the enforcement logic unit-testable without
/// standing up a server.
/// </summary>
public sealed class RbacToolGuard
{
    private readonly ICallerIdentityProvider _identity;
    private readonly IRbacService _rbac;
    private readonly IAuthorizationAuditor _auditor;
    private readonly ILogger<RbacToolGuard> _logger;

    public RbacToolGuard(
        ICallerIdentityProvider identity,
        IRbacService rbac,
        IAuthorizationAuditor auditor,
        ILogger<RbacToolGuard> logger)
    {
        _identity = identity;
        _rbac = rbac;
        _auditor = auditor;
        _logger = logger;
    }

    /// <summary>
    /// Decides whether the current caller may run <paramref name="toolName"/>, records the
    /// decision, and returns the verdict.
    /// </summary>
    public async Task<AuthorizationResult> EvaluateAsync(string toolName, CancellationToken cancellationToken = default)
    {
        var username = _identity.GetCallerName();
        var result = await _rbac.AuthorizeAsync(username, toolName, cancellationToken);

        await _auditor.RecordAsync(username, toolName, result, cancellationToken);

        if (result.IsAllowed)
        {
            _logger.LogInformation("Authorized {User} for tool {Tool}", username, toolName);
        }
        else
        {
            _logger.LogWarning(
                "Denied tool {Tool} for {User} ({Outcome})",
                toolName, username ?? "<anonymous>", result.Outcome);
        }

        return result;
    }

    /// <summary>The tools the current caller is permitted to see during discovery.</summary>
    public async Task<IReadOnlySet<string>> GetVisibleToolsAsync(CancellationToken cancellationToken = default)
    {
        var username = _identity.GetCallerName();
        return await _rbac.GetAuthorizedToolsAsync(username, cancellationToken);
    }
}
