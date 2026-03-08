# MCP RBAC Demo

A working example of a **Model Context Protocol (MCP) server** built with ASP.NET Core that enforces **Role-Based Access Control** on every tool call. The idea is simple: different users get access to different tools depending on their role, and the server rejects anything they shouldn't be touching.

This is meant as a reference project. It's small enough to read in one sitting but covers the patterns you'd actually use in a real system — EF Core for persistence, a clean RBAC service, and the official MCP C# SDK for the protocol layer.

---

## What's inside

The server exposes four MCP tools over HTTP (Streamable HTTP transport). Each tool is gated by role:

| Tool | What it does | Allowed roles |
|------|-------------|---------------|
| `inventory_lookup` | Search products by SKU or name | Admin, Manager |
| `generate_report` | Sales summary for a date range | Manager, Viewer |
| `manage_users` | List, add, or deactivate users | Admin |
| `system_status` | Health check, uptime, runtime info | Admin, Manager, Viewer |

Three demo users are seeded on first run:

| User | Role | Can use |
|------|------|---------|
| `alice` | Admin | All four tools |
| `bob` | Manager | inventory_lookup, generate_report, system_status |
| `carol` | Viewer | generate_report, system_status |

Authentication is intentionally simple — pass an `X-User-Id` header with the username. No JWT, no OAuth. This keeps the focus on the RBAC pattern rather than auth plumbing.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or later)
- That's it. SQLite is embedded — no database server needed.

## Getting started

```bash
cd McpRbacDemo
dotnet run
```

The server starts on `http://localhost:5000` by default (check console output for the actual URL). The MCP endpoint is at `/mcp`.

## Testing with curl

The MCP protocol uses JSON-RPC over HTTP. Here are some quick examples.

**Initialize the session:**

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "X-User-Id: alice" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize",
    "params": {
      "protocolVersion": "2025-03-26",
      "capabilities": {},
      "clientInfo": { "name": "curl-test", "version": "1.0" }
    }
  }'
```

**List available tools:**

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "X-User-Id: alice" \
  -H "Mcp-Session-Id: <session-id-from-init>" \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/list",
    "params": {}
  }'
```

**Call a tool (as alice — Admin):**

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "X-User-Id: alice" \
  -H "Mcp-Session-Id: <session-id-from-init>" \
  -d '{
    "jsonrpc": "2.0",
    "id": 3,
    "method": "tools/call",
    "params": {
      "name": "system_status",
      "arguments": {}
    }
  }'
```

**Try a tool carol can't access:**

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "X-User-Id: carol" \
  -H "Mcp-Session-Id: <session-id-from-init>" \
  -d '{
    "jsonrpc": "2.0",
    "id": 4,
    "method": "tools/call",
    "params": {
      "name": "inventory_lookup",
      "arguments": { "query": "mouse" }
    }
  }'
```

Carol is a Viewer, so this returns an error: *"Access denied. User 'carol' does not have permission to use 'inventory_lookup'."*

## Testing with MCP Inspector

If you have [MCP Inspector](https://github.com/modelcontextprotocol/inspector) installed, point it at `http://localhost:5000/mcp` and set the `X-User-Id` header to one of the demo users. You'll be able to browse tools and call them interactively.

---

## Project structure

```
McpRbacDemo/
  Program.cs                    — App startup, DI wiring, MCP + EF Core config
  appsettings.json              — Connection string and logging config
  Data/
    AppDbContext.cs              — EF Core context (Users, Roles, UserRoles)
    SeedData.cs                 — Creates demo users and roles on first run
  Models/
    User.cs                     — User entity
    Role.cs                     — Role entity
    UserRole.cs                 — Many-to-many join entity
  Services/
    IRbacService.cs             — Interface for the RBAC service
    RbacService.cs              — Role lookups and tool-permission checks
  Tools/
    InventoryTools.cs           — inventory_lookup tool
    ReportTools.cs              — generate_report tool
    UserManagementTools.cs      — manage_users tool
    StatusTools.cs              — system_status tool
  Middleware/
    RbacToolFilter.cs           — MCP CallTool filter that enforces RBAC

McpRbacDemo.Tests/              — xUnit test project (39 tests)
  RbacServiceTests.cs           — Permission map and role lookup tests
  SeedDataTests.cs              — Database seeding and idempotency tests
  InventoryToolsTests.cs        — Inventory search tests
  ReportToolsTests.cs           — Report generation and validation tests
  StatusToolsTests.cs           — System status output tests
  UserManagementToolsTests.cs   — User CRUD operation tests
```

## Running the tests

```bash
dotnet test
```

## How the RBAC works

1. Every HTTP request to the MCP endpoint includes an `X-User-Id` header.
2. When a `tools/call` request arrives, the `RbacToolFilter` fires before the tool executes.
3. The filter reads the header, looks up the user's roles in SQLite via `RbacService`, and checks whether any of those roles are allowed for the requested tool.
4. If the check fails, the filter returns an error result directly — the tool never runs.

The permission map lives in `RbacService.cs` as a simple dictionary. In a real project you'd probably move this to configuration or a database table, but for a demo it keeps things readable.

## Extending it

**Add a new tool:** Create a class in `Tools/`, decorate it with `[McpServerToolType]`, and add methods with `[McpServerTool(Name = "your_tool_name")]`. Then add the tool name and allowed roles to the `ToolPermissions` dictionary in `RbacService.cs`.

**Add a new role:** Insert it in `SeedData.cs` (or at runtime via `manage_users`), then reference it in the permission map.

**Switch to real auth:** Replace the `X-User-Id` header check in `RbacToolFilter` with ASP.NET Core's built-in `[Authorize]` attributes and `AddAuthorizationFilters()`. The MCP C# SDK supports this natively.

---

## Tech stack

- **ASP.NET Core 8** — Web host and HTTP transport
- **[MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk) v1.1.0** — Model Context Protocol implementation
- **Entity Framework Core 8** + **SQLite** — Persistence for users and roles
- **xUnit** + **EF Core InMemory** — Unit testing

## License

This is a demo project. Use it however you like.
