# MCP RBAC Demo

[![CI](https://github.com/allubalu/mcp-rbac-demo/actions/workflows/ci.yml/badge.svg)](https://github.com/allubalu/mcp-rbac-demo/actions/workflows/ci.yml)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

**Role-based access control for AI agent tools.** An ASP.NET Core [Model Context Protocol](https://modelcontextprotocol.io) server where every tool call is authorized against the caller's roles — and every caller sees only the tools they are allowed to run.

When you give a language model tools, "who is allowed to call this?" stops being a UI question and becomes a protocol question. This is a small, complete answer to it: 108 tests, an audit trail, and a policy you change without recompiling.

---

## The idea in one exchange

Carol is a Viewer. She cannot manage users — and the server does not even tell her the tool exists.

```console
$ # carol lists the tools available to her
"name":"generate_report"
"name":"system_status"

$ # carol calls a tool she never saw, by name
Access denied. You do not have permission to use 'manage_users'.

$ # alice, an Admin, reads the audit trail
=== Authorization Log (most recent first) ===
2026-08-30 17:08:14 UTC  ALLOW  alice        audit_log          Allowed
2026-08-30 17:08:14 UTC  DENY   carol        manage_users       DeniedInsufficientRole
```

Alice sees five tools. Bob, a Manager, sees three. Carol sees two. Same server, same endpoint.

---

## Why filter tool discovery, not just execution

Most RBAC examples guard the action. This one guards the catalogue too, and that turns out to matter more than it looks:

- **The model wastes no context on tools it cannot use.** Every tool definition in `tools/list` is tokens spent on every turn. Advertising an Admin-only tool to a Viewer costs money on every request and buys nothing.
- **The model does not plan around a tool it will be refused.** A model that sees `manage_users` will eventually try it, fail, and have to recover. Hiding it removes a whole class of dead-end reasoning.
- **Discovery is a disclosure.** A full tool list tells any caller what the system can do, and by omission, who is privileged enough to do it.

Hiding a tool is a usability and privacy measure. It is **not** the boundary — `tools/call` is. A client that hard-codes a hidden tool name is still refused, and there is [a test for exactly that](McpRbacDemo.Tests/EndToEndRbacTests.cs).

---

## What's inside

Five tools, each gated by role:

| Tool | What it does | Allowed roles |
|------|--------------|---------------|
| `inventory_lookup` | Search products by SKU or name | Admin, Manager |
| `generate_report` | Sales summary for a date range | Admin, Manager, Viewer |
| `manage_users` | List, add, or deactivate users | Admin |
| `audit_log` | Review recent authorization decisions | Admin |
| `system_status` | Health, uptime, runtime info | Admin, Manager, Viewer |

Three users are seeded on first run:

| User | Role | Sees |
|------|------|------|
| `alice` | Admin | all five tools |
| `bob` | Manager | `inventory_lookup`, `generate_report`, `system_status` |
| `carol` | Viewer | `generate_report`, `system_status` |

---

## Getting started

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0). SQLite is embedded — there is no database to install.

```bash
dotnet run --project McpRbacDemo
```

The server listens on `http://localhost:5284` and speaks MCP over Streamable HTTP at `/`.

```bash
dotnet test
```

All 108 tests, no setup required.

### With Docker

```bash
docker build -t mcp-rbac-demo . && docker run -p 8080:8080 mcp-rbac-demo
```

### With MCP Inspector

Point [Inspector](https://github.com/modelcontextprotocol/inspector) at `http://localhost:5284/`, set an `X-User-Id` header to `alice`, `bob`, or `carol`, and watch the tool list change with the header.

---

## How authorization works

```
tools/list ─┐
            ├─▶ RbacToolFilter ─▶ RbacToolGuard ─┬─▶ ICallerIdentityProvider   who is calling?
tools/call ─┘                                    ├─▶ IRbacService              may they? (roles × policy)
                                                 └─▶ IAuthorizationAuditor     write it down
```

1. `ICallerIdentityProvider` resolves the caller. The demo reads a header; see [Threat model](#threat-model).
2. `IRbacService` loads that user's roles from SQLite and checks them against the configured policy.
3. `IAuthorizationAuditor` writes one row per decision — allowed or denied, with the reason.
4. On `tools/call`, a denial short-circuits and the tool never runs. On `tools/list`, unauthorized tools are dropped from the response.

Three properties worth calling out:

**Deny by default.** A tool with no policy entry is unreachable. Adding a tool cannot accidentally expose it.

**A missing grant fails at boot, not at 3am.** Because deny-by-default makes an unconfigured tool silently unreachable, startup cross-checks the tools declared in code against the policy and refuses to start if one was never granted.

**Denials do not leak.** "No such user" and "you lack the role" return the identical message. The audit log records which it was; the caller cannot tell the difference and cannot use the endpoint to enumerate accounts.

### Changing the policy

Permissions live in [`appsettings.json`](McpRbacDemo/appsettings.json), not in code:

```json
"Rbac": {
  "UserIdHeader": "X-User-Id",
  "ToolPermissions": {
    "inventory_lookup": [ "Admin", "Manager" ],
    "manage_users":     [ "Admin" ]
  }
}
```

Granting a role access to a tool is a configuration change. The policy is validated at startup, so a typo — an empty role list, an unknown tool — stops the server rather than silently denying everyone.

---

## Threat model

Worth being explicit, since this is a project about access control.

**In scope, and enforced:** every `tools/call` is authorized; every `tools/list` is filtered; every decision is audited; unknown tools and unknown users are denied; error messages do not reveal whether an account exists.

**Deliberately out of scope:** `X-User-Id` is **not authentication**. Any client can claim to be any user. It stands in for a verified identity so the RBAC layer stays the subject of the demo.

Making it real means replacing one implementation. [`ICallerIdentityProvider`](McpRbacDemo/Services/ICallerIdentityProvider.cs) is the only thing that knows how the caller is identified — swap `HeaderCallerIdentityProvider` for one that reads a validated `ClaimsPrincipal` off an authenticated request, register the SDK's authorization filters, and nothing else in the codebase changes.

Also out of scope: rate limiting, tenant isolation, and audit-log retention. Audit writes are best-effort — a failed write is logged but does not block the call. A system where the audit log is the compliance record would fail closed instead; [the tradeoff is marked in the code](McpRbacDemo/Services/IAuthorizationAuditor.cs).

---

## Project structure

```
McpRbacDemo/
  Program.cs                     App startup, DI wiring, startup policy check
  appsettings.json               Connection string, RBAC policy, logging
  Data/
    AppDbContext.cs              EF Core context
    SeedData.cs                  Demo users and roles
  Models/
    User.cs  Role.cs  UserRole.cs
    AuthorizationAudit.cs        One row per authorization decision
  Services/
    ICallerIdentityProvider.cs   Who is calling — the auth/authz seam
    IRbacService.cs              May they call it? Which tools can they see?
    RbacPolicy.cs                Config-bound policy, map, and startup validator
    RbacToolGuard.cs             Identity + policy + audit, in one place
    IAuthorizationAuditor.cs     Durable record of every decision
    McpToolCatalog.cs            Tools declared in code, for the startup check
  Tools/
    InventoryTools.cs  ReportTools.cs  UserManagementTools.cs
    StatusTools.cs     AuditTools.cs
  Middleware/
    RbacToolFilter.cs            MCP filters for tools/list and tools/call

McpRbacDemo.Tests/               108 tests
  EndToEndRbacTests.cs           Real server, real MCP client, real protocol
  RbacServiceTests.cs            Roles, outcomes, deny-by-default
  RbacPolicyTests.cs             Validation, and config-vs-code drift
  RbacToolGuardTests.cs          Enforcement and auditing
  AuthorizationAuditorTests.cs   Persistence of decisions
  ...                            Per-tool behaviour
```

### On the tests

[`EndToEndRbacTests`](McpRbacDemo.Tests/EndToEndRbacTests.cs) hosts the real application with `WebApplicationFactory` and drives it with a real `McpClient` over the real protocol — so "carol sees two tools" is verified against the server, not a mock.

[`RbacPolicyTests`](McpRbacDemo.Tests/RbacPolicyTests.cs) loads the same `appsettings.json` the app ships with and asserts the policy covers exactly the tools the code declares. Add a tool and forget to grant it, and the test suite says so before the server does.

---

## Extending it

**Add a tool** — create a class in `Tools/`, mark it `[McpServerToolType]`, add a `[McpServerTool(Name = "...")]` method, then grant it in `appsettings.json`. Skip the grant and startup will tell you.

**Add a role** — insert it in `SeedData.cs` or at runtime via `manage_users`, then reference it in the policy.

**Move the policy to a database** — replace `ToolPermissionMap` with a repository-backed implementation. Nothing above it changes.

---

## Built with

[ASP.NET Core 8](https://learn.microsoft.com/aspnet/core) · [MCP C# SDK 1.1.0](https://github.com/modelcontextprotocol/csharp-sdk) · [EF Core 8](https://learn.microsoft.com/ef/core) + SQLite · [xUnit](https://xunit.net)

## License

[MIT](LICENSE)
