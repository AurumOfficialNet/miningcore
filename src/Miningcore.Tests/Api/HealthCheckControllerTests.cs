using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Mvc;
using Miningcore.Api.Controllers;
using Miningcore.Api.Responses;
using Miningcore.Configuration;
using Miningcore.Messaging;
using Miningcore.Persistence;
using Miningcore.Tests.Util;
using Newtonsoft.Json.Serialization;
using NSubstitute;
using Xunit;

namespace Miningcore.Tests.Api;

public class HealthCheckControllerTests
{
    [Fact]
    public async Task GetHealthCheck_ReturnsHealthy_WhenOnlyOptionalChecksExist()
    {
        using var container = BuildContainer(new ClusterConfig
        {
            Pools = new PoolConfig[0],
            Persistence = null
        });

        var sut = new HealthCheckController(container);

        var result = await sut.GetHealthCheck(CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(200, objectResult.StatusCode);

        var payload = Assert.IsType<ReadinessResponse>(objectResult.Value);
        Assert.Equal("healthy", payload.Status);
        Assert.NotNull(payload.TimestampUtc);

        var checks = payload.Checks.ToDictionary(x => x.Name, x => x);
        Assert.Equal("skipped", checks["postgres"].Status);
        Assert.Equal("skipped", checks["redis"].Status);
        Assert.Equal("skipped", checks["daemons"].Status);
        Assert.All(payload.Checks, check => Assert.False(check.Required));
    }

    [Fact]
    public async Task GetHealthCheck_ReturnsUnhealthy_WhenDaemonProbeFails()
    {
        using var container = BuildContainer(new ClusterConfig
        {
            Pools =
            [
                new PoolConfig
                {
                    Id = "acg",
                    Enabled = true,
                    Daemons =
                    [
                        new DaemonEndpointConfig
                        {
                            Host = "127.0.0.1",
                            Port = 1,
                        }
                    ]
                }
            ]
        });

        var sut = new HealthCheckController(container);

    var result = await sut.GetHealthCheck(CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(503, objectResult.StatusCode);

        var payload = Assert.IsType<ReadinessResponse>(objectResult.Value);
        Assert.Equal("unhealthy", payload.Status);

        var daemonCheck = Assert.Single(payload.Checks.Where(x => x.Name == "daemon:acg"));
        Assert.Equal("unhealthy", daemonCheck.Status);
        Assert.True(daemonCheck.Required);
    }

    private static IContainer BuildContainer(ClusterConfig clusterConfig)
    {
        ModuleInitializer.Initialize();

        var builder = new ContainerBuilder();
        var cf = Substitute.For<IConnectionFactory>();

        builder.RegisterInstance(clusterConfig);
        builder.RegisterInstance(cf).As<IConnectionFactory>();
        builder.RegisterInstance(ModuleInitializer.Container.Resolve<AutoMapper.IMapper>()).As<AutoMapper.IMapper>();
        builder.RegisterInstance<IMessageBus>(new MockMessageBus());
        builder.RegisterInstance(new Newtonsoft.Json.JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver()
        });

        return builder.Build();
    }
}
