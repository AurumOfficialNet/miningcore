using Autofac;
using AutoMapper;
using Microsoft.IO;
using Miningcore.Blockchain.Ergo;
using Miningcore.Configuration;
using Miningcore.Persistence.Dummy;
using Miningcore.Stratum;
using Miningcore.Tests.Util;
using Miningcore.Nicehash;
using Newtonsoft.Json;
using NLog;
using Xunit;
#pragma warning disable 8974

namespace Miningcore.Tests.Blockchain.Ergo;

public class ErgoPoolTests : TestBase
{
    [Fact]
    public void Pool_Can_Be_Created_And_Configured()
    {
        // Test that Ergo pool can be created and configured without exceptions
        // This verifies basic pool setup is working
        
        var poolConfig = new PoolConfig { Id = "test", Template = new ErgoCoinTemplate() };
        var clusterConfig = new ClusterConfig();
        var logger = new NullLogger(LogManager.LogFactory);
        var clock = MockMasterClock.FromTicks(638010200200475015);
        var messageBus = new MockMessageBus();
        
        // Test that pool can be created
        var rmsm = new RecyclableMemoryStreamManager();
        var mockStatsRepo = new MockStatsRepository();
        var mapper = container.Resolve<IMapper>();
        var nicehashService = new SimpleNicehashService();
        var pool = new ErgoPool(container, jsonSerializerSettings, new DummyConnectionFactory(""), mockStatsRepo, mapper, clock, messageBus, rmsm, nicehashService);
        pool.Configure(poolConfig, clusterConfig);
        
        // Test passes if no exceptions thrown during creation/configuration
        Assert.True(true);
    }
}
