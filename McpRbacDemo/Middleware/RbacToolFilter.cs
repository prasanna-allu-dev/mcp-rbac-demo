using McpRbacDemo.Services;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpRbacDemo.Middleware;

/// <summary>
/// Registers an MCP CallTool filter that reads the X-User-Id header
/// from the current HTTP request and checks the caller's roles against
/// the RBAC policy before allowing a tool to execute.
/// </summary>
public static class RbacToolFilterExtensions
{
    public static IMcpServerBuilder AddRbacToolFilter(this IMcpServerBuilder builder)
    {
        builder.WithRequestFilters(filters =>
        {
            filters.AddCallToolFilter(next => async (context, cancellationToken) =>
            {
                var logger = context.Services?.GetService<ILogger<RbacMarker>>();
                var rbac = context.Services!.GetRequiredService<IRbacService>();
                var httpAccessor = context.Services!.GetRequiredService<IHttpContextAccessor>();

                var toolName = context.Params?.Name ?? string.Empty;

                var username = httpAccessor.HttpContext?.Request.Headers["X-User-Id"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(username))
                {
                    logger?.LogWarning("Tool call to '{Tool}' rejected — no X-User-Id header", toolName);
                    return new CallToolResult
                    {
                        Content = [new TextContentBlock { Text = "Access denied. Please provide an X-User-Id header." }],
                        IsError = true
                    };
                }

                var authorized = await rbac.IsAuthorizedAsync(username, toolName);
                if (!authorized)
                {
                    logger?.LogWarning("User '{User}' denied access to tool '{Tool}'", username, toolName);
                    return new CallToolResult
                    {
                        Content = [new TextContentBlock { Text = $"Access denied. User '{username}' does not have permission to use '{toolName}'." }],
                        IsError = true
                    };
                }

                logger?.LogInformation("User '{User}' authorized for tool '{Tool}'", username, toolName);
                return await next(context, cancellationToken);
            });
        });

        return builder;
    }

    private class RbacMarker { }
}
