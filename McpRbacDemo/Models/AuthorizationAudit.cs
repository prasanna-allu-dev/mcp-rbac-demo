using McpRbacDemo.Services;

namespace McpRbacDemo.Models;

/// <summary>
/// One durable record per authorization decision. Denials are the interesting rows —
/// a burst of them against one tool is what an access-control system exists to surface.
/// </summary>
public class AuthorizationAudit
{
    public long Id { get; set; }

    public DateTimeOffset TimestampUtc { get; set; }

    /// <summary>The identity the caller claimed, or <c>null</c> when none was presented.</summary>
    public string? Username { get; set; }

    public string ToolName { get; set; } = string.Empty;

    public bool Allowed { get; set; }

    /// <summary>The <see cref="AuthorizationOutcome"/> behind the verdict.</summary>
    public AuthorizationOutcome Outcome { get; set; }
}
