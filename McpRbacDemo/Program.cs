using McpRbacDemo.Data;
using McpRbacDemo.Middleware;
using McpRbacDemo.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- EF Core + SQLite ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- RBAC service ---
builder.Services.AddScoped<IRbacService, RbacService>();

// --- HttpContextAccessor (so the RBAC filter can read headers) ---
builder.Services.AddHttpContextAccessor();

// --- MCP server with HTTP transport and RBAC filter ---
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .AddRbacToolFilter()
    .WithToolsFromAssembly();

var app = builder.Build();

// --- Seed the database on startup ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await SeedData.InitializeAsync(db);
}

// --- Map the MCP endpoint ---
app.MapMcp();

app.Run();
