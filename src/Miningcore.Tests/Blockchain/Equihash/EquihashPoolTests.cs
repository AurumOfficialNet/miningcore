using System.Collections.Generic;
using Autofac;
using AutoMapper;
using Microsoft.IO;
using Miningcore.Blockchain.Equihash;
using Miningcore.Configuration;
using Miningcore.Persistence.Dummy;
using Miningcore.Stratum;
using Miningcore.Tests.Util;
using Miningcore.Nicehash;
using Newtonsoft.Json;
using NLog;
using Xunit;
#pragma warning disable 8974

namespace Miningcore.Tests.Blockchain.Equihash;

public class EquihashPoolTests : TestBase
{
    [Fact]
    public void Pool_Can_Be_Created_And_Configured()
    {
        // Test that Equihash pool can be created and configured without exceptions
        // This verifies basic pool setup is working
        
        var poolConfig = new PoolConfig { Id = "test", Template = new EquihashCoinTemplate() };
        var clusterConfig = new ClusterConfig();
        // Set required z-address for Equihash pool in Extra configuration
        poolConfig.Extra = new Dictionary<string, object>
        {
            ["z-address"] = "t1KJtBGVyAzJhPsv4VwTKxDQs1w3FnLfL7M"
        };
        var logger = new NullLogger(LogManager.LogFactory);
        var clock = MockMasterClock.FromTicks(638010200200475015);
        var messageBus = new MockMessageBus();
        
        // Test that pool can be created
        var rmsm = new RecyclableMemoryStreamManager();
        var mockStatsRepo = new MockStatsRepository();
        var mapper = container.Resolve<IMapper>();
        var nicehashService = new SimpleNicehashService();
        var pool = new EquihashPool(container, jsonSerializerSettings, new DummyConnectionFactory(""), mockStatsRepo, mapper, clock, messageBus, rmsm, nicehashService);
        pool.Configure(poolConfig, clusterConfig);
        
        // Test passes if no exceptions thrown during creation/configuration
        Assert.True(true);
    }
}
