using McpRbacDemo.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpRbacDemo.Middleware;

/// <summary>
/// Wires RBAC into the two MCP requests that matter for tool access:
/// <list type="bullet">
/// <item><c>tools/list</c> — hides tools the caller may not run, so they are never advertised.</item>
/// <item><c>tools/call</c> — blocks execution of tools the caller may not run.</item>
/// </list>
/// <para>
/// Filtering discovery as well as execution is the point. Enforcing only on <c>tools/call</c>
/// still works, but every caller sees the full catalogue: the model burns tokens on tool
/// definitions it can never use, and then picks one and fails. Filtering the list keeps each
/// caller's view of the server honest, while <c>tools/call</c> remains the real boundary —
/// a client that hard-codes a hidden tool name is still denied.
/// </para>
/// </summary>
public static class RbacToolFilterExtensions
{
    public static IMcpServerBuilder AddRbacToolFilter(this IMcpServerBuilder builder)
    {
        builder.WithRequestFilters(filters =>
        {
            filters.AddListToolsFilter(next => async (context, cancellationToken) =>
            {
                var result = await next(context, cancellationToken);

                var guard = context.Services!.GetRequiredService<RbacToolGuard>();
                var visible = await guard.GetVisibleToolsAsync(cancellationToken);

                result.Tools = result.Tools.Where(tool => visible.Contains(tool.Name)).ToList();
                return result;
            });

            filters.AddCallToolFilter(next => async (context, cancellationToken) =>
            {
                var toolName = context.Params?.Name ?? string.Empty;

                var guard = context.Services!.GetRequiredService<RbacToolGuard>();
                var decision = await guard.EvaluateAsync(toolName, cancellationToken);

                if (!decision.IsAllowed)
                {
                    return new CallToolResult
                    {
                        Content = [new TextContentBlock { Text = decision.Message }],
                        IsError = true
                    };
                }

                return await next(context, cancellationToken);
            });
        });

        return builder;
    }
}
