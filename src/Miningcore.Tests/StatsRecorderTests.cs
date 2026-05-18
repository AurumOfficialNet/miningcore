using System;
using System.Collections.Concurrent;
using System.Data;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using AutoMapper;
using Miningcore.Blockchain;
using Miningcore.Configuration;
using Miningcore.Mining;
using Miningcore.Persistence;
using Miningcore.Persistence.Model.Projections;
using Miningcore.Persistence.Repositories;
using Miningcore.Tests.Util;
using NSubstitute;
using Xunit;

namespace Miningcore.Tests;

public class StatsRecorderTests : TestBase
{
    [Fact]
    public async Task UpdatePoolHashratesAsync_TracksConnectedMinersAndWorkersSeparately()
    {
        var clock = new MockMasterClock
        {
            CurrentTime = new DateTime(2026, 5, 17, 12, 0, 0, DateTimeKind.Utc)
        };

        var connection = Substitute.For<IDbConnection>();
        var transaction = Substitute.For<IDbTransaction>();
        connection.BeginTransaction(Arg.Any<IsolationLevel>()).Returns(transaction);

        var connectionFactory = Substitute.For<IConnectionFactory>();
        connectionFactory.OpenConnectionAsync().Returns(Task.FromResult(connection));

        var shareRepo = Substitute.For<IShareRepository>();
        shareRepo.GetHashAccumulationBetweenAsync(Arg.Any<IDbConnection>(), "acg", Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new[]
            {
                new MinerWorkerHashes
                {
                    Miner = "wallet1",
                    Worker = "rig1",
                    Sum = 16,
                    Count = 2,
                    FirstShare = clock.CurrentTime.AddMinutes(-2),
                    LastShare = clock.CurrentTime.AddMinutes(-1)
                },
                new MinerWorkerHashes
                {
                    Miner = "wallet1",
                    Worker = "rig2",
                    Sum = 32,
                    Count = 4,
                    FirstShare = clock.CurrentTime.AddMinutes(-3),
                    LastShare = clock.CurrentTime.AddMinutes(-1)
                }
            }));

        var statsRepo = Substitute.For<IStatsRepository>();
        statsRepo.GetPoolMinerWorkerHashratesAsync(Arg.Any<IDbConnection>(), "acg", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Array.Empty<MinerWorkerHashrate>()));

        var clusterConfig = new ClusterConfig
        {
            Statistics = new Statistics
            {
                UpdateInterval = 120,
                GcInterval = 4,
                HashrateCalculationWindow = 10,
                CleanupDays = 180
            }
        };

        var poolStats = new PoolStats();
        var pool = Substitute.For<IMiningPool>();
        pool.Config.Returns(new PoolConfig { Id = "acg" });
        pool.PoolStats.Returns(poolStats);
        pool.NetworkStats.Returns(new BlockchainStats());
        pool.ShareMultiplier.Returns(1d);
        pool.HashrateFromShares(Arg.Any<double>(), Arg.Any<double>())
            .Returns(call => (double) call[0]);

        var recorder = new StatsRecorder(
            container,
            clock,
            connectionFactory,
            new MockMessageBus(),
            container.Resolve<IMapper>(),
            clusterConfig,
            shareRepo,
            statsRepo);

        var pools = GetPrivatePools(recorder);
        Assert.True(pools.TryAdd("acg", pool));

        await InvokeUpdatePoolHashratesAsync(recorder, CancellationToken.None);

        Assert.Equal(1, poolStats.ConnectedMiners);
        Assert.Equal(2, poolStats.ConnectedWorkers);
    }

    private static ConcurrentDictionary<string, IMiningPool> GetPrivatePools(StatsRecorder recorder)
    {
        var field = typeof(StatsRecorder).GetField("pools", BindingFlags.Instance | BindingFlags.NonPublic);
        return Assert.IsType<ConcurrentDictionary<string, IMiningPool>>(field?.GetValue(recorder));
    }

    private static async Task InvokeUpdatePoolHashratesAsync(StatsRecorder recorder, CancellationToken cancellationToken)
    {
        var method = typeof(StatsRecorder).GetMethod("UpdatePoolHashratesAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        var task = Assert.IsAssignableFrom<Task>(method?.Invoke(recorder, new object[] { cancellationToken }));
        await task;
    }
}