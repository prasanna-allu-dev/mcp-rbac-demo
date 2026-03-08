using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using ModelContextProtocol.Server;

namespace McpRbacDemo.Tools;

[McpServerToolType]
public static class StatusTools
{
    private static readonly DateTime StartedAt = DateTime.UtcNow;

    [McpServerTool(Name = "system_status"), Description("Check system health, uptime, and runtime information.")]
    public static string GetSystemStatus()
    {
        var uptime = DateTime.UtcNow - StartedAt;

        var sb = new StringBuilder();
        sb.AppendLine("=== System Status ===");
        sb.AppendLine($"Status      : Healthy");
        sb.AppendLine($"Uptime      : {FormatUptime(uptime)}");
        sb.AppendLine($"Server Time : {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Runtime     : {RuntimeInformation.FrameworkDescription}");
        sb.AppendLine($"OS          : {RuntimeInformation.OSDescription}");
        using var process = Process.GetCurrentProcess();
        sb.AppendLine($"Process Mem : {process.WorkingSet64 / (1024 * 1024)} MB");

        return sb.ToString();
    }

    private static string FormatUptime(TimeSpan ts)
    {
        if (ts.TotalMinutes < 1) return $"{ts.Seconds}s";
        if (ts.TotalHours < 1)   return $"{ts.Minutes}m {ts.Seconds}s";
        if (ts.TotalDays < 1)    return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        return $"{(int)ts.TotalDays}d {ts.Hours}h {ts.Minutes}m";
    }
}
