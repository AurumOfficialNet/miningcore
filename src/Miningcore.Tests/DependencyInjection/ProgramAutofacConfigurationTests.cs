using System;
using System.Collections.Concurrent;
using Autofac;
using Autofac.Features.ResolveAnything;
using Miningcore.Api.Responses;
using Miningcore.Configuration;
using Miningcore.Mining;
using Miningcore.Persistence;
using Miningcore.Persistence.Dummy;
using Miningcore.Tests.Util;
using Xunit;

namespace Miningcore.Tests.DependencyInjection;

[Collection("ProgramReflectionSerial")]
[Trait("Category", "PreUpgrade")]
public class ProgramAutofacConfigurationTests : TestBase
{
    [Fact]
    public void Program_OriginalAutofacConfiguration_RegistersCoreInstancesAndPersistence()
    {
        ProgramTestReflection.SetProgramLogger();

        var clusterConfig = new ClusterConfig
        {
            ShareRelay = new ShareRelayConfig { PublishUrl = "tcp://127.0.0.1:4001" },
            PaymentProcessing = new ClusterPaymentProcessingConfig { Enabled = false },
            Pools = Array.Empty<PoolConfig>()
        };

        ProgramTestReflection.SetProgramClusterConfig(clusterConfig);

        var builder = new ContainerBuilder();
        builder.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());

        ProgramTestReflection.InvokeProgramPrivateStatic("ConfigureAutofac", builder);

        using var scope = builder.Build();

        Assert.Same(clusterConfig, scope.Resolve<ClusterConfig>());
        Assert.NotNull(scope.Resolve<AdminGcStats>());
        Assert.NotNull(scope.Resolve<ConcurrentDictionary<string, IMiningPool>>());

        var connectionFactory = scope.Resolve<IConnectionFactory>();
        Assert.IsType<DummyConnectionFactory>(connectionFactory);
    }
}