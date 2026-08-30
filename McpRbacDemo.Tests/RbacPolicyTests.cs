using McpRbacDemo.Services;
using McpRbacDemo.Tests.Helpers;

namespace McpRbacDemo.Tests;

public class RbacPolicyTests
{
    private static readonly RbacPolicyValidator Validator = new();

    [Fact]
    public void ShippedPolicy_IsValid()
    {
        var result = Validator.Validate(null, TestPolicy.Shipped());

        Assert.True(result.Succeeded, result.FailureMessage);
    }

    [Fact]
    public void ShippedPolicy_CoversEveryToolTheServerExposes()
    {
        // Guards the deny-by-default trap: a tool added in code but forgotten in configuration
        // is unreachable, and would otherwise only be noticed at runtime.
        var configuredTools = TestPolicy.Shipped().ToolPermissions.Keys;

        Assert.Equal(
            McpToolCatalog.DeclaredToolNames.OrderBy(t => t),
            configuredTools.OrderBy(t => t));
    }

    [Fact]
    public void ShippedPolicy_ReferencesOnlyRolesThatAreSeeded()
    {
        string[] seededRoles = ["Admin", "Manager", "Viewer"];

        var referenced = TestPolicy.Shipped().ToolPermissions.Values
            .SelectMany(roles => roles)
            .Distinct();

        Assert.All(referenced, role => Assert.Contains(role, seededRoles));
    }

    [Fact]
    public void Validator_Rejects_EmptyToolPermissions()
    {
        var result = Validator.Validate(null, new RbacPolicy());

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Validator_Rejects_ToolWithNoRoles()
    {
        var policy = new RbacPolicy
        {
            ToolPermissions = new Dictionary<string, string[]> { ["orphan_tool"] = [] }
        };

        var result = Validator.Validate(null, policy);

        Assert.False(result.Succeeded);
        Assert.Contains("orphan_tool", result.FailureMessage);
    }

    [Fact]
    public void Validator_Rejects_BlankRoleName()
    {
        var policy = new RbacPolicy
        {
            ToolPermissions = new Dictionary<string, string[]> { ["some_tool"] = ["Admin", "  "] }
        };

        Assert.False(Validator.Validate(null, policy).Succeeded);
    }

    [Fact]
    public void Validator_Rejects_EmptyUserIdHeader()
    {
        var policy = TestPolicy.Shipped();
        policy.UserIdHeader = "";

        Assert.False(Validator.Validate(null, policy).Succeeded);
    }

    [Fact]
    public void PermissionMap_MatchesToolNames_CaseInsensitively()
    {
        var map = TestPolicy.MapOf(new Dictionary<string, string[]> { ["some_tool"] = ["Admin"] });

        Assert.True(map.TryGetAllowedRoles("SOME_TOOL", out var roles));
        Assert.Contains("Admin", roles);
    }

    [Fact]
    public void PermissionMap_MatchesRoleNames_CaseInsensitively()
    {
        var map = TestPolicy.MapOf(new Dictionary<string, string[]> { ["some_tool"] = ["Admin"] });

        map.TryGetAllowedRoles("some_tool", out var roles);

        Assert.Contains("aDmIn", roles);
    }

    [Fact]
    public void PermissionMap_ReturnsFalse_ForToolOutsideThePolicy()
    {
        var map = TestPolicy.ShippedMap();

        Assert.False(map.TryGetAllowedRoles("not_a_tool", out _));
    }
}
