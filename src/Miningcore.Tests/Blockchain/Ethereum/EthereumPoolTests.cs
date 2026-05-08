using Autofac;
using Microsoft.IO;
using Miningcore.Blockchain.Ethereum;
using Miningcore.Configuration;
using Miningcore.Stratum;
using Miningcore.Tests.Util;
using Newtonsoft.Json;
using NLog;
using Xunit;
#pragma warning disable 8974

namespace Miningcore.Tests.Blockchain.Ethereum;

public class EthereumPoolTests : TestBase
{
    [Fact]
    public void Pool_Can_Be_Created_And_Configured()
    {
        // Test that Ethereum pool can be created and configured without exceptions
        // This verifies basic pool setup is working
        
        var poolConfig = new PoolConfig { Id = "test", Template = new EthereumCoinTemplate() };
        var clusterConfig = new ClusterConfig();
        var logger = new NullLogger(LogManager.LogFactory);
        var clock = MockMasterClock.FromTicks(638010200200475015);
        var messageBus = new MockMessageBus();
        var rmsm = new RecyclableMemoryStreamManager();
        var pool = new EthereumPool(container, jsonSerializerSettings, null, null, null, clock, messageBus, rmsm, null);
        pool.Configure(poolConfig, clusterConfig);
        
        // Test passes if no exceptions thrown during creation/configuration
        Assert.True(true);
    }
}
