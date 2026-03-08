using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace McpRbacDemo.Tools;

[McpServerToolType]
public static class InventoryTools
{
    private static readonly JsonSerializerOptions IndentedJson = new() { WriteIndented = true };

    private static readonly List<InventoryItem> Catalog =
    [
        new("SKU-1001", "Wireless Mouse",       145, 29.99m),
        new("SKU-1002", "Mechanical Keyboard",    87, 79.99m),
        new("SKU-1003", "USB-C Hub",             210, 49.99m),
        new("SKU-1004", "27\" Monitor",           34, 349.99m),
        new("SKU-1005", "Webcam HD 1080p",       162, 59.99m),
    ];

    [McpServerTool(Name = "inventory_lookup"), Description("Look up product inventory by SKU or partial name. Returns stock level and price.")]
    public static string LookupInventory(
        [Description("SKU code or partial product name to search for")] string query)
    {
        var matches = Catalog
            .Where(item =>
                item.Sku.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                item.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
            return $"No products found matching '{query}'.";

        return JsonSerializer.Serialize(matches, IndentedJson);
    }

    private record InventoryItem(string Sku, string Name, int QuantityOnHand, decimal UnitPrice);
}
