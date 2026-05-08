using Autofac;
using Microsoft.IO;
using Miningcore.Blockchain.Ergo;
using Miningcore.Configuration;
using Miningcore.Stratum;
using Miningcore.Tests.Util;
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
        var pool = new ErgoPool(container, jsonSerializerSettings, null, null, null, clock, messageBus, null, null);
        pool.Configure(poolConfig, clusterConfig);
        
        // Test passes if no exceptions thrown during creation/configuration
        Assert.True(true);
    }
}
