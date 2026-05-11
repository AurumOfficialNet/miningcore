using System;
using System.Threading;
using System.Threading.Tasks;
using Miningcore.Persistence.Repositories;
using Miningcore.Persistence.Model;
using Miningcore.Persistence.Model.Projections;
using System.Data;

namespace Miningcore.Tests.Util;

public class MockStatsRepository : IStatsRepository
{
    public Task InsertPoolStatsAsync(IDbConnection con, IDbTransaction tx, PoolStats stats, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public Task InsertMinerWorkerPerformanceStatsAsync(IDbConnection con, IDbTransaction tx, MinerWorkerPerformanceStats stats, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public Task<PoolStats> GetLastPoolStatsAsync(IDbConnection con, string poolId, CancellationToken ct)
    {
        return Task.FromResult<PoolStats>(null);
    }

    public Task<decimal> GetTotalPoolPaymentsAsync(IDbConnection con, string poolId, CancellationToken ct)
    {
        return Task.FromResult(0m);
    }

    public Task<PoolStats[]> GetPoolPerformanceBetweenAsync(IDbConnection con, string poolId, SampleInterval interval, DateTime start, DateTime end, CancellationToken ct)
    {
        return Task.FromResult<PoolStats[]>(new PoolStats[0]);
    }

    public Task<MinerStats> GetMinerStatsAsync(IDbConnection con, IDbTransaction tx, string poolId, string miner, CancellationToken ct)
    {
        return Task.FromResult<MinerStats>(null);
    }

    public Task<MinerWorkerHashrate[]> GetPoolMinerWorkerHashratesAsync(IDbConnection con, string poolId, CancellationToken ct)
    {
        return Task.FromResult<MinerWorkerHashrate[]>(new MinerWorkerHashrate[0]);
    }

    public Task<MinerWorkerPerformanceStats[]> PagePoolMinersByHashrateAsync(IDbConnection con, string poolId, DateTime from, int page, int pageSize, CancellationToken ct)
    {
        return Task.FromResult<MinerWorkerPerformanceStats[]>(new MinerWorkerPerformanceStats[0]);
    }

    public Task<WorkerPerformanceStatsContainer[]> GetMinerPerformanceBetweenMinutelyAsync(IDbConnection con, string poolId, string miner, DateTime start, DateTime end, CancellationToken ct)
    {
        return Task.FromResult<WorkerPerformanceStatsContainer[]>(new WorkerPerformanceStatsContainer[0]);
    }

    public Task<WorkerPerformanceStatsContainer[]> GetMinerPerformanceBetweenThreeMinutelyAsync(IDbConnection con, string poolId, string miner, DateTime start, DateTime end, CancellationToken ct)
    {
        return Task.FromResult<WorkerPerformanceStatsContainer[]>(new WorkerPerformanceStatsContainer[0]);
    }

    public Task<WorkerPerformanceStatsContainer[]> GetMinerPerformanceBetweenHourlyAsync(IDbConnection con, string poolId, string miner, DateTime start, DateTime end, CancellationToken ct)
    {
        return Task.FromResult<WorkerPerformanceStatsContainer[]>(new WorkerPerformanceStatsContainer[0]);
    }

    public Task<WorkerPerformanceStatsContainer[]> GetMinerPerformanceBetweenDailyAsync(IDbConnection con, string poolId, string miner, DateTime start, DateTime end, CancellationToken ct)
    {
        return Task.FromResult<WorkerPerformanceStatsContainer[]>(new WorkerPerformanceStatsContainer[0]);
    }

    public Task<int> DeletePoolStatsBeforeAsync(IDbConnection con, DateTime date, CancellationToken ct)
    {
        return Task.FromResult(0);
    }

    public Task<int> DeleteMinerStatsBeforeAsync(IDbConnection con, DateTime date, CancellationToken ct)
    {
        return Task.FromResult(0);
    }
}
