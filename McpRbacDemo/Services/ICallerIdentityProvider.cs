using Microsoft.Extensions.Options;

namespace McpRbacDemo.Services;

/// <summary>
/// Resolves who is making the current MCP request.
/// <para>
/// This is the seam between authentication and authorization. The demo ships a header-based
/// implementation to keep the focus on RBAC; moving to OAuth or OIDC means replacing this
/// single implementation and touching nothing else.
/// </para>
/// </summary>
public interface ICallerIdentityProvider
{
    /// <summary>The caller's username, or <c>null</c> when the request carries no identity.</summary>
    string? GetCallerName();

    /// <summary>How the caller is expected to identify themselves. Used in error messages.</summary>
    string IdentitySource { get; }
}

/// <summary>
/// Reads the caller's username from a request header.
/// <para>
/// <b>Demo only.</b> A header is trivially spoofable and is not authentication — any client can
/// claim to be any user. It stands in for a verified identity so the RBAC layer stays the subject.
/// In production this would be replaced by a provider that reads a validated
/// <c>ClaimsPrincipal</c> off the authenticated request.
/// </para>
/// </summary>
public sealed class HeaderCallerIdentityProvider : ICallerIdentityProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _headerName;

    public HeaderCallerIdentityProvider(IHttpContextAccessor httpContextAccessor, IOptions<RbacPolicy> policy)
    {
        _httpContextAccessor = httpContextAccessor;
        _headerName = policy.Value.UserIdHeader;
    }

    public string IdentitySource => _headerName;

    public string? GetCallerName()
    {
        var value = _httpContextAccessor.HttpContext?.Request.Headers[_headerName].FirstOrDefault();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
