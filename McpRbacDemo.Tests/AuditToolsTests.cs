using McpRbacDemo.Models;
using McpRbacDemo.Services;
using McpRbacDemo.Tests.Helpers;
using McpRbacDemo.Tools;

namespace McpRbacDemo.Tests;

public class AuditToolsTests
{
    private static async Task<McpRbacDemo.Data.AppDbContext> SeedAuditAsync()
    {
        var db = TestDbContextFactory.Create();
        db.AuthorizationAudits.AddRange(
            new AuthorizationAudit
            {
                TimestampUtc = new DateTimeOffset(2026, 3, 8, 10, 0, 0, TimeSpan.Zero),
                Username = "alice",
                ToolName = "system_status",
                Allowed = true,
                Outcome = AuthorizationOutcome.Allowed
            },
            new AuthorizationAudit
            {
                TimestampUtc = new DateTimeOffset(2026, 3, 8, 11, 0, 0, TimeSpan.Zero),
                Username = "carol",
                ToolName = "manage_users",
                Allowed = false,
                Outcome = AuthorizationOutcome.DeniedInsufficientRole
            });
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task ReadAuditLog_ReturnsBothVerdicts_ByDefault()
    {
        await using var db = await SeedAuditAsync();

        var output = await AuditTools.ReadAuditLog(db);

        Assert.Contains("ALLOW", output);
        Assert.Contains("DENY", output);
    }

    [Fact]
    public async Task ReadAuditLog_FiltersToDenialsOnly()
    {
        await using var db = await SeedAuditAsync();

        var output = await AuditTools.ReadAuditLog(db, deniedOnly: true);

        Assert.Contains("carol", output);
        Assert.DoesNotContain("ALLOW", output);
    }

    [Fact]
    public async Task ReadAuditLog_IncludesTheDenialReason()
    {
        await using var db = await SeedAuditAsync();

        var output = await AuditTools.ReadAuditLog(db, deniedOnly: true);

        Assert.Contains(nameof(AuthorizationOutcome.DeniedInsufficientRole), output);
    }

    [Fact]
    public async Task ReadAuditLog_ShowsNewestFirst()
    {
        await using var db = await SeedAuditAsync();

        var output = await AuditTools.ReadAuditLog(db);

        Assert.True(output.IndexOf("carol", StringComparison.Ordinal) < output.IndexOf("alice", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ReadAuditLog_HonoursTheLimit()
    {
        await using var db = await SeedAuditAsync();

        var output = await AuditTools.ReadAuditLog(db, limit: 1);

        Assert.Contains("carol", output);
        Assert.DoesNotContain("alice", output);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(10_000)]
    public async Task ReadAuditLog_ClampsOutOfRangeLimits(int limit)
    {
        await using var db = await SeedAuditAsync();

        var output = await AuditTools.ReadAuditLog(db, limit: limit);

        Assert.Contains("carol", output);
    }

    [Fact]
    public async Task ReadAuditLog_ReportsAnEmptyLog()
    {
        await using var db = TestDbContextFactory.Create();

        Assert.Contains("No authorization activity", await AuditTools.ReadAuditLog(db));
    }

    [Fact]
    public async Task ReadAuditLog_ReportsNoDenials_WhenEverythingWasAllowed()
    {
        await using var db = TestDbContextFactory.Create();
        db.AuthorizationAudits.Add(new AuthorizationAudit
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Username = "alice",
            ToolName = "system_status",
            Allowed = true,
            Outcome = AuthorizationOutcome.Allowed
        });
        await db.SaveChangesAsync();

        Assert.Contains("No denied attempts", await AuditTools.ReadAuditLog(db, deniedOnly: true));
    }

    [Fact]
    public async Task ReadAuditLog_LabelsAnonymousAttempts()
    {
        await using var db = TestDbContextFactory.Create();
        db.AuthorizationAudits.Add(new AuthorizationAudit
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Username = null,
            ToolName = "system_status",
            Allowed = false,
            Outcome = AuthorizationOutcome.DeniedNoIdentity
        });
        await db.SaveChangesAsync();

        Assert.Contains("<anonymous>", await AuditTools.ReadAuditLog(db));
    }
}
