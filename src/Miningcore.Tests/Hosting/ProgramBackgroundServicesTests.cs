using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Miningcore.Configuration;
using Miningcore.Mining;
using Miningcore.Notifications;
using Miningcore.Payments;
using Miningcore.Tests.Util;
using Xunit;

namespace Miningcore.Tests.Hosting;

[Collection("ProgramReflectionSerial")]
[Trait("Category", "PreUpgrade")]
public class ProgramBackgroundServicesTests : TestBase
{
    [Fact]
    public void Program_OriginalServiceConfiguration_WithoutRelay_RegistersBackgroundProcessors()
    {
        ProgramTestReflection.SetProgramLogger();
        ProgramTestReflection.SetProgramClusterConfig(new ClusterConfig
        {
            ShareRelay = null,
            Api = new ApiConfig { Enabled = true },
            PaymentProcessing = new ClusterPaymentProcessingConfig { Enabled = true },
            Pools = new[]
            {
                new PoolConfig
                {
                    PaymentProcessing = new PoolPaymentProcessingConfig { Enabled = true }
                }
            }
        });

        var services = new ServiceCollection();
        ProgramTestReflection.InvokeProgramPrivateStatic("ConfigureBackgroundServices", services);

        AssertHostedServiceRegistered<NotificationService>(services);
        AssertHostedServiceRegistered<BtStreamReceiver>(services);
        AssertHostedServiceRegistered<ShareRecorder>(services);
        AssertHostedServiceRegistered<ShareReceiver>(services);
        AssertHostedServiceRegistered<StatsRecorder>(services);
        AssertHostedServiceRegistered<BlockchainIndexer>(services);
        AssertHostedServiceRegistered<MetricsPublisher>(services);
        AssertHostedServiceRegistered<PayoutManager>(services);
        AssertHostedServiceNotRegistered<ShareRelay>(services);
    }

    [Fact]
    public void Program_OriginalServiceConfiguration_WithRelay_UsesShareRelayPath()
    {
        ProgramTestReflection.SetProgramLogger();
        ProgramTestReflection.SetProgramClusterConfig(new ClusterConfig
        {
            ShareRelay = new ShareRelayConfig { PublishUrl = "tcp://127.0.0.1:4001" },
            Api = new ApiConfig { Enabled = false },
            PaymentProcessing = new ClusterPaymentProcessingConfig { Enabled = false },
            Pools = Array.Empty<PoolConfig>()
        });

        var services = new ServiceCollection();
        ProgramTestReflection.InvokeProgramPrivateStatic("ConfigureBackgroundServices", services);

        AssertHostedServiceRegistered<NotificationService>(services);
        AssertHostedServiceRegistered<BtStreamReceiver>(services);
        AssertHostedServiceRegistered<ShareRelay>(services);

        AssertHostedServiceNotRegistered<ShareRecorder>(services);
        AssertHostedServiceNotRegistered<ShareReceiver>(services);
        AssertHostedServiceNotRegistered<StatsRecorder>(services);
        AssertHostedServiceNotRegistered<BlockchainIndexer>(services);
        AssertHostedServiceNotRegistered<MetricsPublisher>(services);
        AssertHostedServiceNotRegistered<PayoutManager>(services);
    }

    private static void AssertHostedServiceRegistered<THosted>(IServiceCollection services)
        where THosted : class, IHostedService
    {
        Assert.Contains(services, x => x.ServiceType == typeof(IHostedService) && x.ImplementationType == typeof(THosted));
    }

    private static void AssertHostedServiceNotRegistered<THosted>(IServiceCollection services)
        where THosted : class, IHostedService
    {
        Assert.DoesNotContain(services, x => x.ServiceType == typeof(IHostedService) && x.ImplementationType == typeof(THosted));
    }
}