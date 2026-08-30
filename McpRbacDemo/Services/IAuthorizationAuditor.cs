using McpRbacDemo.Data;
using McpRbacDemo.Models;

namespace McpRbacDemo.Services;

/// <summary>Persists authorization decisions so they can be reviewed after the fact.</summary>
public interface IAuthorizationAuditor
{
    Task RecordAsync(string? username, string toolName, AuthorizationResult result, CancellationToken cancellationToken = default);
}

/// <summary>Writes each authorization decision to the database.</summary>
public sealed class AuthorizationAuditor : IAuthorizationAuditor
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AuthorizationAuditor> _logger;

    public AuthorizationAuditor(AppDbContext db, TimeProvider timeProvider, ILogger<AuthorizationAuditor> logger)
    {
        _db = db;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task RecordAsync(
        string? username,
        string toolName,
        AuthorizationResult result,
        CancellationToken cancellationToken = default)
    {
        _db.AuthorizationAudits.Add(new AuthorizationAudit
        {
            TimestampUtc = _timeProvider.GetUtcNow(),
            Username = username,
            ToolName = toolName,
            Allowed = result.IsAllowed,
            Outcome = result.Outcome
        });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Deliberate choice: a failed audit write is logged but does not fail the call.
            // A system where audit storage is the source of truth for compliance would instead
            // fail closed here — deny the call rather than lose the record.
            _logger.LogError(ex, "Failed to persist authorization audit for tool {Tool}", toolName);
        }
    }
}
