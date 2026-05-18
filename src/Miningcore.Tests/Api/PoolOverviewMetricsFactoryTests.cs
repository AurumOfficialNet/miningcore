using System;
using Miningcore.Api;
using Miningcore.Api.Responses;
using Miningcore.Blockchain;
using Miningcore.Mining;
using Miningcore.Persistence.Model.Projections;
using Xunit;

namespace Miningcore.Tests.Api;

public class PoolOverviewMetricsFactoryTests
{
    [Fact]
    public void Create_UsesSchedulerNextRun_WhenNoLastPayoutExists()
    {
        var now = new DateTime(2026, 5, 18, 12, 0, 0, DateTimeKind.Utc);
        var schedulerNextRun = now.AddMinutes(10);

        var result = PoolOverviewMetricsFactory.Create(BuildPoolInfo(500, 1000), new PoolShareSummary
        {
            ShareCount = 42,
            ShareDifficulty = 123.5,
        }, null, now, schedulerNextRun, 600);

        Assert.Equal(42, result.ShareCount24h);
        Assert.Equal(123.5, result.ShareDifficulty24h, 3);
        Assert.Equal(0.5, Assert.IsType<double>(result.NetworkShare), 3);
        Assert.Equal(schedulerNextRun, result.NextPayoutEta);
        Assert.Equal("scheduler", result.NextPayoutEtaSource);
    }

    [Fact]
    public void Create_UsesIntervalEta_WhenLastPayoutPlusIntervalIsInFuture()
    {
        var now = new DateTime(2026, 5, 18, 12, 0, 0, DateTimeKind.Utc);
        var lastPayout = now.AddMinutes(-5);

        var result = PoolOverviewMetricsFactory.Create(BuildPoolInfo(500, 1000), new PoolShareSummary(),
            lastPayout, now, now.AddMinutes(10), 600);

        Assert.Equal(now.AddMinutes(5), result.NextPayoutEta);
        Assert.Equal("interval", result.NextPayoutEtaSource);
    }

    [Fact]
    public void Create_FallsBackToScheduler_WhenComputedEtaIsStale()
    {
        var now = new DateTime(2026, 5, 18, 12, 0, 0, DateTimeKind.Utc);
        var schedulerNextRun = now.AddMinutes(3);

        var result = PoolOverviewMetricsFactory.Create(BuildPoolInfo(500, 1000), new PoolShareSummary(),
            now.AddMinutes(-30), now, schedulerNextRun, 600);

        Assert.Equal(schedulerNextRun, result.NextPayoutEta);
        Assert.Equal("scheduler-fallback", result.NextPayoutEtaSource);
    }

    [Fact]
    public void Create_ReturnsNullNetworkShare_WhenNetworkHashrateIsZero()
    {
        var result = PoolOverviewMetricsFactory.Create(BuildPoolInfo(500, 0), new PoolShareSummary(),
            null, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(10), 600);

        Assert.Null(result.NetworkShare);
    }

    private static PoolInfo BuildPoolInfo(ulong poolHashrate, double networkHashrate)
    {
        return new PoolInfo
        {
            PoolStats = new PoolStats
            {
                PoolHashrate = poolHashrate,
            },
            NetworkStats = new BlockchainStats
            {
                NetworkHashrate = networkHashrate,
            }
        };
    }
}