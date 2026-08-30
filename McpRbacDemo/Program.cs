using McpRbacDemo.Data;
using McpRbacDemo.Middleware;
using McpRbacDemo.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// --- EF Core + SQLite ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- RBAC policy, bound from the "Rbac" configuration section ---
// ValidateOnStart turns a malformed policy into a startup failure rather than
// a runtime surprise where every call is silently denied.
builder.Services
    .AddOptions<RbacPolicy>()
    .Bind(builder.Configuration.GetSection(RbacPolicy.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<RbacPolicy>, RbacPolicyValidator>();
builder.Services.AddSingleton<ToolPermissionMap>();

// --- Authorization pipeline ---
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ICallerIdentityProvider, HeaderCallerIdentityProvider>();
builder.Services.AddScoped<IRbacService, RbacService>();
builder.Services.AddScoped<IAuthorizationAuditor, AuthorizationAuditor>();
builder.Services.AddScoped<RbacToolGuard>();

// --- MCP server with HTTP transport and RBAC on tool discovery + execution ---
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .AddRbacToolFilter()
    .WithToolsFromAssembly();

var app = builder.Build();

// --- Fail fast if a declared tool has no policy entry ---
// Unknown tools are denied by default, so a missing entry is not a security hole —
// it is a silently unreachable tool. Better to hear about it at boot.
var declaredButUngranted = McpToolCatalog.FindToolsMissingFromPolicy(
    app.Services.GetRequiredService<ToolPermissionMap>());
if (declaredButUngranted.Count > 0)
{
    throw new InvalidOperationException(
        $"These tools are declared in code but absent from the '{RbacPolicy.SectionName}:{nameof(RbacPolicy.ToolPermissions)}' " +
        $"policy, so nobody can call them: {string.Join(", ", declaredButUngranted)}.");
}

// --- Seed the demo users and roles on startup ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await SeedData.InitializeAsync(db);
}

// --- Map the MCP endpoint ---
app.MapMcp();

app.Run();

/// <summary>Exposed so the integration tests can host the app with <c>WebApplicationFactory</c>.</summary>
public partial class Program;
