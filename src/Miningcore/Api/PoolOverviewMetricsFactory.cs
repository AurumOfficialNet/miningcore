using Miningcore.Api.Responses;
using Miningcore.Persistence.Model.Projections;

namespace Miningcore.Api;

public static class PoolOverviewMetricsFactory
{
    public static PoolOverviewMetrics Create(PoolInfo poolInfo, PoolShareSummary shareSummary,
        DateTime? lastPoolPayout, DateTime now, DateTime? schedulerNextRun, int payoutIntervalSeconds)
    {
        var networkHashrate = poolInfo.NetworkStats?.NetworkHashrate ?? 0;
        double? networkShare = null;

        if(networkHashrate > 0)
            networkShare = poolInfo.PoolStats?.PoolHashrate / networkHashrate;

        var overview = new PoolOverviewMetrics
        {
            ShareCount24h = shareSummary.ShareCount,
            ShareDifficulty24h = shareSummary.ShareDifficulty,
            NetworkShare = networkShare,
            LastPoolPayout = lastPoolPayout,
            SchedulerNextPayoutRun = schedulerNextRun,
        };

        if(!lastPoolPayout.HasValue)
        {
            overview.NextPayoutEta = schedulerNextRun;
            overview.NextPayoutEtaSource = schedulerNextRun.HasValue ? "scheduler" : null;
            return overview;
        }

        var effectiveIntervalSeconds = payoutIntervalSeconds > 0 ? payoutIntervalSeconds : 600;
        var eta = lastPoolPayout.Value.Add(TimeSpan.FromSeconds(effectiveIntervalSeconds));

        if(eta < now)
        {
            overview.NextPayoutEta = schedulerNextRun;
            overview.NextPayoutEtaSource = schedulerNextRun.HasValue ? "scheduler-fallback" : null;
            return overview;
        }

        overview.NextPayoutEta = eta;
        overview.NextPayoutEtaSource = "interval";

        return overview;
    }
}