using McpRbacDemo.Tools;

namespace McpRbacDemo.Tests;

public class StatusToolsTests
{
    [Fact]
    public void GetSystemStatus_ReturnsHealthyStatus()
    {
        var result = StatusTools.GetSystemStatus();

        Assert.Contains("Healthy", result);
    }

    [Fact]
    public void GetSystemStatus_IncludesRuntimeInfo()
    {
        var result = StatusTools.GetSystemStatus();

        Assert.Contains("Runtime", result);
        Assert.Contains("OS", result);
        Assert.Contains("Process Mem", result);
    }

    [Fact]
    public void GetSystemStatus_IncludesUptime()
    {
        var result = StatusTools.GetSystemStatus();

        Assert.Contains("Uptime", result);
    }
}
