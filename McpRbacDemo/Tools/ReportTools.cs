using System.ComponentModel;
using System.Globalization;
using System.Text;
using ModelContextProtocol.Server;

namespace McpRbacDemo.Tools;

[McpServerToolType]
public static class ReportTools
{
    [McpServerTool(Name = "generate_report"), Description("Generate a sales summary report for a given date range. Dates should be in yyyy-MM-dd format.")]
    public static string GenerateReport(
        [Description("Start date in yyyy-MM-dd format")] string startDate,
        [Description("End date in yyyy-MM-dd format")] string endDate)
    {
        if (!DateOnly.TryParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var from))
            return $"Invalid start date '{startDate}'. Expected format: yyyy-MM-dd.";

        if (!DateOnly.TryParseExact(endDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var to))
            return $"Invalid end date '{endDate}'. Expected format: yyyy-MM-dd.";

        if (from > to)
            return "Start date must be on or before the end date.";

        var days = to.DayNumber - from.DayNumber + 1;
        var rng = new Random(from.DayNumber ^ to.DayNumber);

        var totalOrders = rng.Next(80, 320) * days;
        var totalRevenue = Math.Round(totalOrders * (decimal)(rng.NextDouble() * 40 + 20), 2);
        var avgOrderValue = Math.Round(totalRevenue / totalOrders, 2);

        var sb = new StringBuilder();
        sb.AppendLine($"=== Sales Summary Report ===");
        sb.AppendLine($"Period       : {from:yyyy-MM-dd} to {to:yyyy-MM-dd} ({days} day(s))");
        sb.AppendLine($"Total Orders : {totalOrders:N0}");
        sb.AppendLine($"Total Revenue: ${totalRevenue:N2}");
        sb.AppendLine($"Avg Order    : ${avgOrderValue:N2}");
        sb.AppendLine();
        sb.AppendLine("Top Categories:");
        sb.AppendLine($"  1. Electronics  — ${Math.Round(totalRevenue * 0.42m, 2):N2}");
        sb.AppendLine($"  2. Accessories  — ${Math.Round(totalRevenue * 0.31m, 2):N2}");
        sb.AppendLine($"  3. Peripherals  — ${Math.Round(totalRevenue * 0.27m, 2):N2}");

        return sb.ToString();
    }
}
