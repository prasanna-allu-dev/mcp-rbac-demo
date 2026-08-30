using McpRbacDemo.Services;
using McpRbacDemo.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace McpRbacDemo.Tests;

public class AuthorizationAuditorTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 8, 12, 30, 0, TimeSpan.Zero);

    private static AuthorizationAuditor CreateAuditor(McpRbacDemo.Data.AppDbContext db) =>
        new(db, new FakeTimeProvider(FixedNow), NullLogger<AuthorizationAuditor>.Instance);

    [Fact]
    public async Task RecordAsync_PersistsAnAllowedDecision()
    {
        await using var db = TestDbContextFactory.Create();
        var auditor = CreateAuditor(db);

        await auditor.RecordAsync("alice", "manage_users", AuthorizationResult.Allow());

        var row = await db.AuthorizationAudits.SingleAsync();
        Assert.Equal("alice", row.Username);
        Assert.Equal("manage_users", row.ToolName);
        Assert.True(row.Allowed);
        Assert.Equal(AuthorizationOutcome.Allowed, row.Outcome);
        Assert.Equal(FixedNow, row.TimestampUtc);
    }

    [Fact]
    public async Task RecordAsync_PersistsTheDenialReason()
    {
        await using var db = TestDbContextFactory.Create();
        var auditor = CreateAuditor(db);

        await auditor.RecordAsync("carol", "manage_users", AuthorizationResult.InsufficientRole("manage_users"));

        var row = await db.AuthorizationAudits.SingleAsync();
        Assert.False(row.Allowed);
        Assert.Equal(AuthorizationOutcome.DeniedInsufficientRole, row.Outcome);
    }

    [Fact]
    public async Task RecordAsync_PersistsAnonymousAttempts()
    {
        await using var db = TestDbContextFactory.Create();
        var auditor = CreateAuditor(db);

        await auditor.RecordAsync(null, "system_status", AuthorizationResult.NoIdentity("X-User-Id"));

        var row = await db.AuthorizationAudits.SingleAsync();
        Assert.Null(row.Username);
        Assert.Equal(AuthorizationOutcome.DeniedNoIdentity, row.Outcome);
    }

    [Fact]
    public async Task RecordAsync_KeepsOneRowPerAttempt()
    {
        await using var db = TestDbContextFactory.Create();
        var auditor = CreateAuditor(db);

        await auditor.RecordAsync("carol", "manage_users", AuthorizationResult.InsufficientRole("manage_users"));
        await auditor.RecordAsync("carol", "manage_users", AuthorizationResult.InsufficientRole("manage_users"));
        await auditor.RecordAsync("carol", "inventory_lookup", AuthorizationResult.InsufficientRole("inventory_lookup"));

        Assert.Equal(3, await db.AuthorizationAudits.CountAsync());
        Assert.Equal(2, await db.AuthorizationAudits.CountAsync(a => a.ToolName == "manage_users"));
    }
}
