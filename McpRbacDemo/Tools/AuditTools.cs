using System.ComponentModel;
using System.Text;
using McpRbacDemo.Data;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace McpRbacDemo.Tools;

[McpServerToolType]
public static class AuditTools
{
    private const int MaxLimit = 200;

    [McpServerTool(Name = "audit_log"), Description("Review recent authorization decisions. Shows who called which tool, and whether it was allowed or denied.")]
    public static async Task<string> ReadAuditLog(
        AppDbContext db,
        [Description("Maximum number of entries to return (1-200, default 20)")] int limit = 20,
        [Description("Set true to show only denied attempts")] bool deniedOnly = false,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, MaxLimit);

        var query = db.AuthorizationAudits.AsQueryable();
        if (deniedOnly)
            query = query.Where(entry => !entry.Allowed);

        var entries = await query
            .OrderByDescending(entry => entry.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

        if (entries.Count == 0)
            return deniedOnly ? "No denied attempts recorded." : "No authorization activity recorded yet.";

        var sb = new StringBuilder();
        sb.AppendLine(deniedOnly ? "=== Denied Attempts (most recent first) ===" : "=== Authorization Log (most recent first) ===");
        foreach (var entry in entries)
        {
            var verdict = entry.Allowed ? "ALLOW" : "DENY ";
            sb.AppendLine(
                $"{entry.TimestampUtc:yyyy-MM-dd HH:mm:ss} UTC  {verdict}  " +
                $"{entry.Username ?? "<anonymous>",-12} {entry.ToolName,-18} {entry.Outcome}");
        }

        return sb.ToString();
    }
}
