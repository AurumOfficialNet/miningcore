namespace Miningcore.Api.Responses;

public class AdminOverviewResponse
{
    public AdminPoolHealth PoolHealth { get; set; }
    public AdminBackendHealth BackendHealth { get; set; }
}

public class AdminPoolHealth
{
    public string StratumStatus { get; set; }
    public int MinerCount { get; set; }
    public double ShareRate { get; set; }
    public double StaleRate { get; set; }
}

public class AdminBackendHealth
{
    public long MiningcoreUptimeSeconds { get; set; }
    public string RedisStatus { get; set; }
    public string PostgresStatus { get; set; }
    public int ApiLatencyMs { get; set; }
}
