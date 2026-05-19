using System.Diagnostics;
using Autofac;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Miningcore.Api.Responses;
using Miningcore.Configuration;
using Miningcore.Extensions;
using Miningcore.Messaging;
using Miningcore.Rpc;
using Newtonsoft.Json.Linq;
using NLog;

namespace Miningcore.Api.Controllers;

[Route("api")]
[ApiController]
public class HealthCheckController(IComponentContext ctx) : ApiControllerBase(ctx)
{
    private readonly IMessageBus messageBus = ctx.Resolve<IMessageBus>();
    private readonly Newtonsoft.Json.JsonSerializerSettings serializerSettings = ctx.Resolve<Newtonsoft.Json.JsonSerializerSettings>();

    private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

    [HttpGet("health-check")]
    [Produces("application/json")]
    public async Task<ActionResult<ReadinessResponse>> GetHealthCheck(CancellationToken ct)
    {
        var checks = new List<DependencyCheckResult>();

        // TODO: Add external probe companion status (outside-in pool endpoint reachability) to this response.

        checks.Add(await CheckPostgresAsync(ct));
        checks.Add(CheckRedis());
        checks.AddRange(await CheckDaemonsAsync(ct));

        var healthy = checks
            .Where(x => x.Required)
            .All(x => x.Status == "healthy");

        var response = new ReadinessResponse
        {
            Status = healthy ? "healthy" : "unhealthy",
            TimestampUtc = DateTime.UtcNow,
            Checks = checks.ToArray(),
        };

        return StatusCode(healthy ? 200 : 503, response);
    }

    private async Task<DependencyCheckResult> CheckPostgresAsync(CancellationToken ct)
    {
        if(clusterConfig.Persistence?.Postgres == null)
        {
            return new DependencyCheckResult
            {
                Name = "postgres",
                Status = "skipped",
                Required = false,
                Details = "Postgres is not configured",
            };
        }

        var sw = Stopwatch.StartNew();

        try
        {
            using var probeCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            probeCts.CancelAfter(TimeSpan.FromSeconds(3));

            var result = await cf.Run(con => con.ExecuteScalarAsync<int>(new CommandDefinition("SELECT 1", cancellationToken: probeCts.Token)));

            return new DependencyCheckResult
            {
                Name = "postgres",
                Status = result == 1 ? "healthy" : "unhealthy",
                Required = true,
                DurationMs = sw.ElapsedMilliseconds,
                Details = result == 1 ? "SELECT 1 succeeded" : "SELECT 1 returned unexpected value",
            };
        }
        catch(Exception ex)
        {
            return new DependencyCheckResult
            {
                Name = "postgres",
                Status = "unhealthy",
                Required = true,
                DurationMs = sw.ElapsedMilliseconds,
                Details = ex.Message,
            };
        }
    }

    private static DependencyCheckResult CheckRedis()
    {
        return new DependencyCheckResult
        {
            Name = "redis",
            Status = "skipped",
            Required = false,
            Details = "Redis is not configured in this codebase",
        };
    }

    private async Task<DependencyCheckResult[]> CheckDaemonsAsync(CancellationToken ct)
    {
        var enabledPools = clusterConfig.Pools
            .Where(x => x.Enabled)
            .ToArray();

        if(enabledPools.Length == 0)
        {
            return
            [
                new DependencyCheckResult
                {
                    Name = "daemons",
                    Status = "skipped",
                    Required = false,
                    Details = "No enabled pools",
                }
            ];
        }

        var result = new List<DependencyCheckResult>();

        foreach(var pool in enabledPools)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                if(pool.Daemons == null || pool.Daemons.Length == 0)
                {
                    result.Add(new DependencyCheckResult
                    {
                        Name = $"daemon:{pool.Id}",
                        Status = "unhealthy",
                        Required = true,
                        DurationMs = sw.ElapsedMilliseconds,
                        Details = "No daemon endpoint configured",
                    });

                    continue;
                }

                var endpoint = pool.Daemons.FirstOrDefault(x => string.IsNullOrEmpty(x.Category)) ?? pool.Daemons.First();
                var rpc = new RpcClient(endpoint, serializerSettings, messageBus, pool.Id);

                using var probeCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                probeCts.CancelAfter(TimeSpan.FromSeconds(3));

                var probe = await ProbeDaemonAsync(rpc, pool.Template?.Family, probeCts.Token);

                result.Add(new DependencyCheckResult
                {
                    Name = $"daemon:{pool.Id}",
                    Status = probe.ok ? "healthy" : "unhealthy",
                    Required = true,
                    DurationMs = sw.ElapsedMilliseconds,
                    Details = probe.message,
                });
            }
            catch(Exception ex)
            {
                result.Add(new DependencyCheckResult
                {
                    Name = $"daemon:{pool.Id}",
                    Status = "unhealthy",
                    Required = true,
                    DurationMs = sw.ElapsedMilliseconds,
                    Details = ex.Message,
                });
            }
        }

        return result.ToArray();
    }

    private async Task<(bool ok, string message)> ProbeDaemonAsync(RpcClient rpc, CoinFamily? family, CancellationToken ct)
    {
        var probeMethods = family switch
        {
            CoinFamily.Cryptonote => new[] { "get_info", "getblockchaininfo", "getinfo" },
            CoinFamily.Ethereum => new[] { "eth_blockNumber", "eth_syncing", "net_peerCount" },
            _ => new[] { "getblockchaininfo", "getinfo", "get_info" },
        };

        string lastError = "No probe method executed";

        foreach(var method in probeMethods)
        {
            var rpcResult = await rpc.ExecuteAsync<JToken>(logger, method, ct);

            if(rpcResult.Error == null)
                return (true, $"RPC method '{method}' succeeded");

            lastError = $"RPC method '{method}' failed: {rpcResult.Error.Message}";
        }

        return (false, lastError);
    }
}
