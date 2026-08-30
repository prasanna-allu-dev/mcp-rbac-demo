using McpRbacDemo.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;

namespace McpRbacDemo.Tests.Helpers;

/// <summary>
/// Hosts the real application in memory and connects a real MCP client to it, so the
/// end-to-end tests exercise the actual protocol rather than a mock of it.
/// </summary>
internal sealed class McpTestServer : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Swap SQLite for an isolated in-memory store; everything else stays as configured.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }

    /// <summary>Opens an MCP session that identifies itself as <paramref name="username"/>.</summary>
    public async Task<McpClient> ConnectAsync(string? username, CancellationToken cancellationToken = default)
    {
        var options = new HttpClientTransportOptions
        {
            Endpoint = new Uri("http://localhost/"),
            TransportMode = HttpTransportMode.StreamableHttp
        };

        if (username is not null)
        {
            options.AdditionalHeaders = new Dictionary<string, string> { ["X-User-Id"] = username };
        }

        var transport = new HttpClientTransport(options, CreateClient(), NullLoggerFactory.Instance, ownsHttpClient: true);
        return await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
    }

    /// <summary>Reads the audit rows the server wrote, newest first.</summary>
    public async Task<List<McpRbacDemo.Models.AuthorizationAudit>> ReadAuditLogAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.AuthorizationAudits
            .OrderByDescending(a => a.Id)
            .ToListAsync();
    }
}
