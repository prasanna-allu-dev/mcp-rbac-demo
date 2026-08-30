namespace McpRbacDemo.Services;

/// <summary>
/// Why a tool call was allowed or denied. Kept explicit rather than a bare bool so
/// the audit trail records the reason, not just the verdict.
/// </summary>
public enum AuthorizationOutcome
{
    Allowed,

    /// <summary>No caller identity was presented on the request.</summary>
    DeniedNoIdentity,

    /// <summary>The caller is unknown to the system, or has been deactivated.</summary>
    DeniedUnknownPrincipal,

    /// <summary>The tool has no entry in the permission policy. Unknown tools are denied by default.</summary>
    DeniedUnknownTool,

    /// <summary>The caller is known, but holds none of the roles the tool requires.</summary>
    DeniedInsufficientRole
}

/// <summary>
/// The verdict for a single tool call, together with a caller-safe message.
/// </summary>
/// <param name="Outcome">The specific reason for the verdict.</param>
/// <param name="Message">Text safe to return to the caller. Never leaks the policy contents.</param>
public sealed record AuthorizationResult(AuthorizationOutcome Outcome, string Message)
{
    public bool IsAllowed => Outcome == AuthorizationOutcome.Allowed;

    public static AuthorizationResult Allow() =>
        new(AuthorizationOutcome.Allowed, "Authorized.");

    public static AuthorizationResult NoIdentity(string headerName) =>
        new(AuthorizationOutcome.DeniedNoIdentity,
            $"Access denied. Provide a caller identity via the '{headerName}' header.");

    public static AuthorizationResult UnknownPrincipal(string toolName) =>
        new(AuthorizationOutcome.DeniedUnknownPrincipal,
            $"Access denied. You do not have permission to use '{toolName}'.");

    public static AuthorizationResult UnknownTool(string toolName) =>
        new(AuthorizationOutcome.DeniedUnknownTool,
            $"Access denied. No policy is defined for '{toolName}'.");

    public static AuthorizationResult InsufficientRole(string toolName) =>
        new(AuthorizationOutcome.DeniedInsufficientRole,
            $"Access denied. You do not have permission to use '{toolName}'.");
}
