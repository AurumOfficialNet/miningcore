using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Miningcore.Api.Middlewares;
using Miningcore.Configuration;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Miningcore.Tests.Api;

public class AdminApiAuthorizationMiddlewareTests
{
    [Fact]
    public async Task AdminPath_Rejects_WhenVerifyHeaderMissing()
    {
        var ct = TestContext.Current.CancellationToken;

        using var host = await BuildHostAsync(new ApiConfig
        {
            AdminTrustedProxyIpWhitelist = new string[0],
            AdminClientAllowSubjects = new[] { "CN=Admin User" },
        }, ct);

        var client = host.GetTestClient();
        var response = await client.GetAsync("/api/admin/capabilities", ct);
        var payload = await ReadJsonAsync(response, ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("Admin access denied", payload.Value<string>("error"));
    }

    [Fact]
    public async Task AdminPath_Rejects_WhenIdentityIsNotAllowlisted()
    {
        var ct = TestContext.Current.CancellationToken;

        using var host = await BuildHostAsync(new ApiConfig
        {
            AdminTrustedProxyIpWhitelist = new string[0],
            AdminClientAllowSubjects = new[] { "CN=Admin User" },
            AdminClientAllowSerials = new[] { "00AA11" },
        }, ct);

        var client = host.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/capabilities");
        request.Headers.TryAddWithoutValidation("X-Admin-Client-Verify", "SUCCESS");
        request.Headers.TryAddWithoutValidation("X-Admin-Client-Subject", "CN=Different User");
        request.Headers.TryAddWithoutValidation("X-Admin-Client-Serial", "FFEE22");

        var response = await client.SendAsync(request, ct);
        var payload = await ReadJsonAsync(response, ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("Admin access denied", payload.Value<string>("error"));
    }

    [Fact]
    public async Task AdminPath_Allows_WhenSubjectMatchesAllowlist()
    {
        var clusterConfig = new ClusterConfig
        {
            Api = new ApiConfig
            {
                AdminTrustedProxyIpWhitelist = new string[0],
                AdminClientAllowSubjects = new[] { "CN=Admin User" },
            },
            Logging = new ClusterLoggingConfig(),
        };

        var nextCalled = false;
        var middleware = new AdminApiAuthorizationMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, clusterConfig);

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Loopback;
        context.Request.Path = "/api/admin/capabilities";
        context.Request.Headers["X-Admin-Client-Verify"] = "SUCCESS";
        context.Request.Headers["X-Admin-Client-Subject"] = "CN=Admin User";

        await middleware.Invoke(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task AdminPath_Rejects_WhenProxyNotTrusted()
    {
        var ct = TestContext.Current.CancellationToken;

        using var host = await BuildHostAsync(new ApiConfig
        {
            AdminTrustedProxyIpWhitelist = new[] { "10.10.10.10" },
            AdminClientAllowSubjects = new[] { "CN=Admin User" },
        }, ct);

        var client = host.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/capabilities");
        request.Headers.TryAddWithoutValidation("X-Admin-Client-Verify", "SUCCESS");
        request.Headers.TryAddWithoutValidation("X-Admin-Client-Subject", "CN=Admin User");

        var response = await client.SendAsync(request, ct);
        var payload = await ReadJsonAsync(response, ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("Admin access denied", payload.Value<string>("error"));
    }

    [Fact]
    public async Task NonAdminPath_IsNotBlockedByMiddleware()
    {
        var ct = TestContext.Current.CancellationToken;

        using var host = await BuildHostAsync(new ApiConfig
        {
            AdminTrustedProxyIpWhitelist = new[] { "10.10.10.10" },
            AdminClientAllowSubjects = new[] { "CN=Admin User" },
        }, ct);

        var client = host.GetTestClient();
        var response = await client.GetAsync("/health", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<IHost> BuildHostAsync(ApiConfig apiConfig, CancellationToken ct)
    {
        var clusterConfig = new ClusterConfig
        {
            Api = apiConfig,
            Logging = new ClusterLoggingConfig(),
        };

        var builder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddSingleton(clusterConfig);
                });

                webHost.Configure(app =>
                {
                    app.UseMiddleware<ApiExceptionHandlingMiddleware>();
                    app.UseMiddleware<AdminApiAuthorizationMiddleware>();

                    app.Map("/api/admin/capabilities", adminApp =>
                    {
                        adminApp.Run(async context =>
                        {
                            context.Response.StatusCode = (int) HttpStatusCode.OK;
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsync("{\"canAccessAdmin\":true,\"permissions\":[\"overview\",\"blocks\",\"logs\"]}");
                        });
                    });

                    app.Map("/health", healthApp =>
                    {
                        healthApp.Run(context =>
                        {
                            context.Response.StatusCode = (int) HttpStatusCode.OK;
                            return Task.CompletedTask;
                        });
                    });
                });
            });

        return await builder.StartAsync(ct);
    }

    private static async Task<JObject> ReadJsonAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        return string.IsNullOrEmpty(body) ? new JObject() : JObject.Parse(body);
    }
}
