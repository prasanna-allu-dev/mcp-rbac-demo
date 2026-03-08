using McpRbacDemo.Tools;

namespace McpRbacDemo.Tests;

public class ReportToolsTests
{
    [Fact]
    public void GenerateReport_ReturnsValidReport_ForValidDates()
    {
        var result = ReportTools.GenerateReport("2025-01-01", "2025-01-31");

        Assert.Contains("Sales Summary Report", result);
        Assert.Contains("Total Orders", result);
        Assert.Contains("Total Revenue", result);
        Assert.Contains("Top Categories", result);
    }

    [Fact]
    public void GenerateReport_ReturnsError_ForInvalidStartDate()
    {
        var result = ReportTools.GenerateReport("not-a-date", "2025-01-31");

        Assert.Contains("Invalid start date", result);
    }

    [Fact]
    public void GenerateReport_ReturnsError_ForInvalidEndDate()
    {
        var result = ReportTools.GenerateReport("2025-01-01", "bad");

        Assert.Contains("Invalid end date", result);
    }

    [Fact]
    public void GenerateReport_ReturnsError_WhenStartAfterEnd()
    {
        var result = ReportTools.GenerateReport("2025-02-01", "2025-01-01");

        Assert.Contains("Start date must be on or before", result);
    }

    [Fact]
    public void GenerateReport_IsDeterministic_ForSameDates()
    {
        var r1 = ReportTools.GenerateReport("2025-03-01", "2025-03-15");
        var r2 = ReportTools.GenerateReport("2025-03-01", "2025-03-15");

        Assert.Equal(r1, r2);
    }
}
