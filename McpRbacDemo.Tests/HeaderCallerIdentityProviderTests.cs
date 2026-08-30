using McpRbacDemo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace McpRbacDemo.Tests;

public class HeaderCallerIdentityProviderTests
{
    private static HeaderCallerIdentityProvider CreateProvider(
        Action<HttpContext>? configureRequest = null,
        string headerName = "X-User-Id")
    {
        var accessor = new HttpContextAccessor();

        if (configureRequest is not null)
        {
            var context = new DefaultHttpContext();
            configureRequest(context);
            accessor.HttpContext = context;
        }

        var policy = Options.Create(new RbacPolicy { UserIdHeader = headerName });
        return new HeaderCallerIdentityProvider(accessor, policy);
    }

    [Fact]
    public void GetCallerName_ReadsTheConfiguredHeader()
    {
        var provider = CreateProvider(ctx => ctx.Request.Headers["X-User-Id"] = "alice");

        Assert.Equal("alice", provider.GetCallerName());
    }

    [Fact]
    public void GetCallerName_HonoursACustomHeaderName()
    {
        var provider = CreateProvider(
            ctx => ctx.Request.Headers["X-Acme-Principal"] = "bob",
            headerName: "X-Acme-Principal");

        Assert.Equal("bob", provider.GetCallerName());
    }

    [Fact]
    public void GetCallerName_TrimsSurroundingWhitespace()
    {
        var provider = CreateProvider(ctx => ctx.Request.Headers["X-User-Id"] = "  alice  ");

        Assert.Equal("alice", provider.GetCallerName());
    }

    [Fact]
    public void GetCallerName_ReturnsNull_WhenHeaderIsAbsent()
    {
        var provider = CreateProvider(_ => { });

        Assert.Null(provider.GetCallerName());
    }

    [Fact]
    public void GetCallerName_ReturnsNull_WhenHeaderIsBlank()
    {
        var provider = CreateProvider(ctx => ctx.Request.Headers["X-User-Id"] = "   ");

        Assert.Null(provider.GetCallerName());
    }

    [Fact]
    public void GetCallerName_ReturnsNull_OutsideAnHttpRequest()
    {
        var provider = CreateProvider();

        Assert.Null(provider.GetCallerName());
    }

    [Fact]
    public void IdentitySource_ReportsTheConfiguredHeader()
    {
        var provider = CreateProvider(headerName: "X-Acme-Principal");

        Assert.Equal("X-Acme-Principal", provider.IdentitySource);
    }
}
