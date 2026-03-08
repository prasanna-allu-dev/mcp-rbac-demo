using McpRbacDemo.Tools;

namespace McpRbacDemo.Tests;

public class InventoryToolsTests
{
    [Fact]
    public void LookupInventory_FindsBySku()
    {
        var result = InventoryTools.LookupInventory("SKU-1001");

        Assert.Contains("Wireless Mouse", result);
        Assert.Contains("29.99", result);
    }

    [Fact]
    public void LookupInventory_FindsByPartialName()
    {
        var result = InventoryTools.LookupInventory("keyboard");

        Assert.Contains("Mechanical Keyboard", result);
    }

    [Fact]
    public void LookupInventory_IsCaseInsensitive()
    {
        var result = InventoryTools.LookupInventory("MOUSE");

        Assert.Contains("Wireless Mouse", result);
    }

    [Fact]
    public void LookupInventory_ReturnsNotFound_ForNoMatch()
    {
        var result = InventoryTools.LookupInventory("nonexistent-product-xyz");

        Assert.Contains("No products found", result);
    }

    [Fact]
    public void LookupInventory_ReturnsMultipleMatches()
    {
        var result = InventoryTools.LookupInventory("SKU");

        Assert.Contains("SKU-1001", result);
        Assert.Contains("SKU-1005", result);
    }
}
