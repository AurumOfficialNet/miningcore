using System;
using System.Collections.Concurrent;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Miningcore.Blockchain;
using Miningcore.Configuration;
using Miningcore.Mining;
using Miningcore.Persistence;
using Miningcore.Persistence.Repositories;
using Miningcore.Tests.Util;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Xunit;

namespace Miningcore.Tests.Mining;

public class BlockchainIndexerTests : TestBase
{
    // ------------------------------------------------------------------ //
    //  ResolveAddress – private static helper
    // ------------------------------------------------------------------ //

    private static string InvokeResolveAddress(JObject scriptPubKey)
    {
        var method = typeof(BlockchainIndexer)
            .GetMethod("ResolveAddress", BindingFlags.NonPublic | BindingFlags.Static);
        return (string) method!.Invoke(null, new object[] { scriptPubKey });
    }

    [Fact]
    public void ResolveAddress_WithAddressField_ReturnsAddress()
    {
        var spk = JObject.FromObject(new { address = "addr123" });
        Assert.Equal("addr123", InvokeResolveAddress(spk));
    }

    [Fact]
    public void ResolveAddress_WithAddressesArray_ReturnsFirstElement()
    {
        var spk = new JObject
        {
            ["addresses"] = new JArray("addr-first", "addr-second")
        };
        Assert.Equal("addr-first", InvokeResolveAddress(spk));
    }

    [Fact]
    public void ResolveAddress_PrefersAddressOverAddressesArray()
    {
        var spk = new JObject
        {
            ["address"] = "primary",
            ["addresses"] = new JArray("secondary")
        };
        Assert.Equal("primary", InvokeResolveAddress(spk));
    }

    [Fact]
    public void ResolveAddress_EmptyObject_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, InvokeResolveAddress(new JObject()));
    }

    [Fact]
    public void ResolveAddress_Null_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, InvokeResolveAddress(null));
    }

    // ------------------------------------------------------------------ //
    //  Pool-family filtering – eligible vs skipped
    // ------------------------------------------------------------------ //

    [Theory]
    [InlineData(CoinFamily.Bitcoin)]
    [InlineData(CoinFamily.Equihash)]
    public void IndexPoolAsync_BitcoinEquihashFamilies_AreEligible(CoinFamily family)
    {
        var pool = MakePool(family);
        var eligible = IsEligiblePool(pool);
        Assert.True(eligible, $"Expected {family} pool to be eligible");
    }

    [Theory]
    [InlineData(CoinFamily.Cryptonote)]
    [InlineData(CoinFamily.Ethereum)]
    [InlineData(CoinFamily.Ergo)]
    public void IndexPoolAsync_NonBitcoinFamilies_AreSkipped(CoinFamily family)
    {
        var pool = MakePool(family);
        var eligible = IsEligiblePool(pool);
        Assert.False(eligible, $"Expected {family} pool to be skipped");
    }

    // ------------------------------------------------------------------ //
    //  Coinbase detection
    // ------------------------------------------------------------------ //

    [Fact]
    public void CoinbaseDetection_VinWithCoinbaseField_IsDetectedAsCoinbase()
    {
        var tx = new JObject
        {
            ["txid"] = "txabc",
            ["vin"] = new JArray(new JObject { ["coinbase"] = "deadbeef" })
        };

        Assert.True(IsCoinbase(tx));
    }

    [Fact]
    public void CoinbaseDetection_VinWithoutCoinbaseField_IsNotCoinbase()
    {
        var tx = new JObject
        {
            ["txid"] = "txabc",
            ["vin"] = new JArray(new JObject
            {
                ["txid"] = "prev",
                ["vout"] = 0
            })
        };

        Assert.False(IsCoinbase(tx));
    }

    [Fact]
    public void CoinbaseDetection_EmptyVin_IsNotCoinbase()
    {
        var tx = new JObject
        {
            ["txid"] = "txabc",
            ["vin"] = new JArray()
        };

        Assert.False(IsCoinbase(tx));
    }

    // ------------------------------------------------------------------ //
    //  ExecuteAsync skips pools with no daemons configured
    // ------------------------------------------------------------------ //

    [Fact]
    public async Task ExecuteAsync_PoolWithNoDaemons_IsSkippedWithoutException()
    {
        var clock = new MockMasterClock
        {
            CurrentTime = new DateTime(2026, 5, 17, 12, 0, 0, DateTimeKind.Utc)
        };

        var connectionFactory = Substitute.For<IConnectionFactory>();
        var miningTxRepo = Substitute.For<IMinerTransactionRepository>();

        var clusterConfig = new ClusterConfig
        {
            Pools = new[]
            {
                new PoolConfig
                {
                    Id = "btc1",
                    Enabled = true,
                    Daemons = Array.Empty<DaemonEndpointConfig>()   // no daemons
                }
            }
        };

        var indexer = new BlockchainIndexer(
            container,
            clock,
            connectionFactory,
            new MockMessageBus(),
            clusterConfig,
            jsonSerializerSettings,
            miningTxRepo);

        var ct = TestContext.Current.CancellationToken;

        // Should not throw; daemon-less pools are simply not processed
        await indexer.StartAsync(ct);
        await Task.Delay(250, ct).ContinueWith(_ => { }, CancellationToken.None);
        await indexer.StopAsync(CancellationToken.None);

        // No DB calls should have been made for a pool with no daemons
        await miningTxRepo.DidNotReceive().GetLastIndexedHeightAsync(
            Arg.Any<IDbConnection>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ------------------------------------------------------------------ //
    //  Helpers
    // ------------------------------------------------------------------ //

    private static PoolConfig MakePool(CoinFamily family)
    {
        var template = new TestCoinTemplate { Family = family };
        return new PoolConfig
        {
            Id = "test",
            Enabled = true,
            Daemons = new[]
            {
                new DaemonEndpointConfig { Host = "127.0.0.1", Port = 8332 }
            },
            Template = template
        };
    }

    private static bool IsEligiblePool(PoolConfig pool)
    {
        return pool.Template?.Family == CoinFamily.Bitcoin ||
               pool.Template?.Family == CoinFamily.Equihash;
    }

    private static bool IsCoinbase(JObject tx)
    {
        return tx["vin"] is JArray vin && vin.Count > 0 &&
               vin[0] is JObject firstVin && firstVin["coinbase"] != null;
    }

    private sealed class TestCoinTemplate : CoinTemplate
    {
        public override string GetAlgorithmName()
        {
            return string.Empty;
        }
    }
}
