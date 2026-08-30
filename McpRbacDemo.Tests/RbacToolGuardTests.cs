using McpRbacDemo.Services;
using McpRbacDemo.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpRbacDemo.Tests;

public class RbacToolGuardTests
{
    private static (RbacToolGuard Guard, RecordingAuditor Auditor) CreateGuard(
        McpRbacDemo.Data.AppDbContext db,
        string? caller)
    {
        var identity = new StubCallerIdentityProvider(caller);
        var rbac = new RbacService(db, TestPolicy.ShippedMap(), identity);
        var auditor = new RecordingAuditor();
        var guard = new RbacToolGuard(identity, rbac, auditor, NullLogger<RbacToolGuard>.Instance);
        return (guard, auditor);
    }

    [Fact]
    public async Task EvaluateAsync_AllowsPermittedCall()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();
        var (guard, _) = CreateGuard(db, "alice");

        var result = await guard.EvaluateAsync("manage_users");

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task EvaluateAsync_DeniesUnpermittedCall()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();
        var (guard, _) = CreateGuard(db, "carol");

        var result = await guard.EvaluateAsync("manage_users");

        Assert.False(result.IsAllowed);
    }

    [Fact]
    public async Task EvaluateAsync_AuditsAllowedCalls()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();
        var (guard, auditor) = CreateGuard(db, "alice");

        await guard.EvaluateAsync("manage_users");

        var record = Assert.Single(auditor.Records);
        Assert.Equal("alice", record.Username);
        Assert.Equal("manage_users", record.ToolName);
        Assert.True(record.Result.IsAllowed);
    }

    [Fact]
    public async Task EvaluateAsync_AuditsDeniedCalls_WithTheReason()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();
        var (guard, auditor) = CreateGuard(db, "carol");

        await guard.EvaluateAsync("manage_users");

        var record = Assert.Single(auditor.Records);
        Assert.False(record.Result.IsAllowed);
        Assert.Equal(AuthorizationOutcome.DeniedInsufficientRole, record.Result.Outcome);
    }

    [Fact]
    public async Task EvaluateAsync_AuditsAnonymousAttempts()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();
        var (guard, auditor) = CreateGuard(db, caller: null);

        await guard.EvaluateAsync("system_status");

        var record = Assert.Single(auditor.Records);
        Assert.Null(record.Username);
        Assert.Equal(AuthorizationOutcome.DeniedNoIdentity, record.Result.Outcome);
    }

    [Fact]
    public async Task GetVisibleToolsAsync_HidesToolsTheCallerCannotRun()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();
        var (guard, _) = CreateGuard(db, "carol");

        var visible = await guard.GetVisibleToolsAsync();

        Assert.Equal(["generate_report", "system_status"], visible.OrderBy(t => t));
    }

    [Fact]
    public async Task GetVisibleToolsAsync_ReturnsNothing_ForAnonymousCaller()
    {
        await using var db = await TestDbContextFactory.CreateSeededAsync();
        var (guard, _) = CreateGuard(db, caller: null);

        Assert.Empty(await guard.GetVisibleToolsAsync());
    }

    [Fact]
    public async Task GetVisibleToolsAsync_DoesNotWriteAuditRows()
    {
        // Discovery is not an access attempt. Auditing every tools/list would bury
        // the denials that actually matter.
        await using var db = await TestDbContextFactory.CreateSeededAsync();
        var (guard, auditor) = CreateGuard(db, "carol");

        await guard.GetVisibleToolsAsync();

        Assert.Empty(auditor.Records);
    }
}
