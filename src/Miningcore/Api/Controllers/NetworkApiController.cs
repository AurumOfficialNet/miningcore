using Autofac;
using Microsoft.AspNetCore.Mvc;
using Miningcore.Api.Responses;
using Miningcore.Blockchain.Bitcoin;
using Miningcore.Configuration;
using Miningcore.Extensions;
using Miningcore.Messaging;
using Miningcore.Persistence.Model;
using Miningcore.Persistence.Model.Projections;
using Miningcore.Persistence.Repositories;
using Miningcore.Rpc;
using Miningcore.Time;
using Newtonsoft.Json.Linq;
using NLog;
using System.Globalization;
using System.Net;

namespace Miningcore.Api.Controllers;

[Route("api/network/{poolId}")]
[ApiController]
[Produces("application/json")]
public class NetworkApiController : ApiControllerBase
{
    public NetworkApiController(IComponentContext ctx) : base(ctx)
    {
        statsRepo = ctx.Resolve<IStatsRepository>();
        balanceRepo = ctx.Resolve<IBalanceRepository>();
        messageBus = ctx.Resolve<IMessageBus>();
        clock = ctx.Resolve<IMasterClock>();
        serializerSettings = ctx.Resolve<Newtonsoft.Json.JsonSerializerSettings>();
    }

    private readonly IStatsRepository statsRepo;
    private readonly IBalanceRepository balanceRepo;
    private readonly IMessageBus messageBus;
    private readonly IMasterClock clock;
    private readonly Newtonsoft.Json.JsonSerializerSettings serializerSettings;

    private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

    [HttpGet("overview")]
    public async Task<ActionResult<NetworkOverviewResponse>> GetOverviewAsync(string poolId, CancellationToken ct)
    {
        var pool = GetPool(poolId);
        var rpc = CreateRpcClient(pool);

        var stats = await cf.Run(con => statsRepo.GetLastPoolStatsAsync(con, pool.Id, ct));
        var blockchainInfo = await ExecuteObjectAsync(rpc, BitcoinCommands.GetBlockchainInfo, null, ct);
        var daemonHeight = blockchainInfo.Value<ulong?>("blocks") ?? 0;

        var totalSupply = await ExecuteDecimalAsync(rpc, "gettxoutsetinfo", null, ct, "total_amount");

        var blockHeight = stats != null ? (ulong) stats.BlockHeight : daemonHeight;
        var networkDifficulty = stats?.NetworkDifficulty ?? blockchainInfo.Value<double?>("difficulty") ?? 0;
        var networkHashrate = stats?.NetworkHashrate ?? await ExecuteDoubleAsync(rpc, BitcoinCommands.GetNetworkHashPS, null, ct);
        var blockTimeSeconds = await GetAverageBlockTimeSecondsAsync(rpc, daemonHeight, ct);

        var now = clock.Now;
        var start = now > DateTime.MinValue.AddHours(24) ? now.AddHours(-24) : now;
        var samples = stats == null
            ? Array.Empty<PoolStats>()
            : await cf.Run(con => statsRepo.GetPoolPerformanceBetweenAsync(con, pool.Id, SampleInterval.Hour, start, now, ct));

        return new NetworkOverviewResponse
        {
            NetworkHashrate = networkHashrate,
            NetworkDifficulty = networkDifficulty,
            BlockHeight = blockHeight,
            BlockTimeSeconds = blockTimeSeconds,
            TotalSupply = totalSupply,
            HashrateSeries = samples.Select(x => new NetworkSeriesPoint
            {
                Timestamp = x.Created.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
                Value = x.NetworkHashrate
            }).ToArray(),
            DifficultySeries = samples.Select(x => new NetworkSeriesPoint
            {
                Timestamp = x.Created.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
                Value = x.NetworkDifficulty
            }).ToArray()
        };
    }

    [HttpGet("blocks")]
    public async Task<ActionResult<NetworkBlockSummaryResponse[]>> GetBlocksAsync(string poolId, CancellationToken ct)
    {
        var pool = GetPool(poolId);
        var rpc = CreateRpcClient(pool);

        var latestHeight = await GetLatestBlockHeightAsync(rpc, ct);
        var startHeight = latestHeight >= 99 ? latestHeight - 99 : 0;

        var blocks = new List<NetworkBlockSummaryResponse>();

        for(long height = (long) latestHeight; height >= (long) startHeight; height--)
        {
            var block = await GetBlockByHeightAsync(rpc, height, ct);

            blocks.Add(new NetworkBlockSummaryResponse
            {
                Height = block.Value<ulong>("height"),
                Hash = block.Value<string>("hash"),
                Timestamp = ToIsoTimestamp(block.Value<long?>("time")),
                TxCount = block["tx"] is JArray txs ? txs.Count : 0,
                Reward = GetCoinbaseReward(block)
            });

            if(height == 0)
                break;
        }

        return blocks.ToArray();
    }

    [HttpGet("block/{height:long}")]
    public async Task<ActionResult<NetworkBlockDetailResponse>> GetBlockAsync(string poolId, long height, CancellationToken ct)
    {
        if(height < 0)
            throw new ApiException("Invalid block height", HttpStatusCode.BadRequest);

        var pool = GetPool(poolId);
        var rpc = CreateRpcClient(pool);

        var block = await GetBlockByHeightAsync(rpc, height, ct);
        var transactions = await MapTransactionsAsync(rpc, block, ct);

        return new NetworkBlockDetailResponse
        {
            Height = block.Value<ulong>("height"),
            Hash = block.Value<string>("hash"),
            PrevHash = block.Value<string>("previousblockhash"),
            Timestamp = ToIsoTimestamp(block.Value<long?>("time")),
            Difficulty = block.Value<double?>("difficulty") ?? 0,
            Reward = GetCoinbaseReward(block),
            Transactions = transactions
        };
    }

    [HttpGet("top100")]
    public async Task<ActionResult<NetworkTopHolderResponse[]>> GetTop100Async(string poolId, CancellationToken ct)
    {
        var pool = GetPool(poolId);
        var totalSupply = await GetTotalSupplyAsync(pool, ct);

        var balances = await cf.Run(con => balanceRepo.GetPoolBalancesOverThresholdAsync(con, pool.Id, 0m));

        return balances
            .OrderByDescending(x => x.Amount)
            .Take(100)
            .Select((x, index) => new NetworkTopHolderResponse
            {
                Rank = index + 1,
                Address = x.Address,
                Balance = x.Amount,
                PercentOfSupply = totalSupply > 0 ? x.Amount / totalSupply * 100m : 0m
            })
            .ToArray();
    }

    private RpcClient CreateRpcClient(PoolConfig pool)
    {
        return new RpcClient(pool.Daemons.First(), serializerSettings, messageBus, pool.Id);
    }

    private async Task<JObject> ExecuteObjectAsync(RpcClient rpc, string method, object payload, CancellationToken ct)
    {
        var result = await rpc.ExecuteAsync<JObject>(logger, method, ct, payload);

        if(result.Error != null)
            throw new ApiException($"Daemon RPC {method} failed: {result.Error.Message}", HttpStatusCode.BadGateway);

        return result.Response ?? throw new ApiException($"Daemon RPC {method} returned no data", HttpStatusCode.BadGateway);
    }

    private async Task<double> ExecuteDoubleAsync(RpcClient rpc, string method, object payload, CancellationToken ct)
    {
        var result = await rpc.ExecuteAsync<JToken>(logger, method, ct, payload);

        if(result.Error != null)
            throw new ApiException($"Daemon RPC {method} failed: {result.Error.Message}", HttpStatusCode.BadGateway);

        return result.Response?.Value<double>() ?? 0;
    }

    private async Task<decimal> ExecuteDecimalAsync(RpcClient rpc, string method, object payload, CancellationToken ct, string propertyName = null)
    {
        var result = await rpc.ExecuteAsync<JObject>(logger, method, ct, payload);

        if(result.Error != null)
            throw new ApiException($"Daemon RPC {method} failed: {result.Error.Message}", HttpStatusCode.BadGateway);

        if(propertyName != null)
            return result.Response?.Value<decimal?>(propertyName) ?? 0m;

        return result.Response?.Value<decimal?>() ?? 0m;
    }

    private async Task<ulong> GetLatestBlockHeightAsync(RpcClient rpc, CancellationToken ct)
    {
        var blockchainInfo = await ExecuteObjectAsync(rpc, BitcoinCommands.GetBlockchainInfo, null, ct);
        return blockchainInfo.Value<ulong?>("blocks") ?? 0;
    }

    private async Task<JObject> GetBlockByHeightAsync(RpcClient rpc, long height, CancellationToken ct)
    {
        var hashResult = await rpc.ExecuteAsync<string>(logger, "getblockhash", ct, new object[] { height });

        if(hashResult.Error != null)
            throw new ApiException($"Daemon RPC getblockhash failed: {hashResult.Error.Message}", HttpStatusCode.BadGateway);

        var blockHash = hashResult.Response;

        var blockResult = await rpc.ExecuteAsync<JObject>(logger, BitcoinCommands.GetBlock, ct, new object[] { blockHash, 2 });

        if(blockResult.Error != null)
            throw new ApiException($"Daemon RPC getblock failed: {blockResult.Error.Message}", HttpStatusCode.BadGateway);

        return blockResult.Response ?? throw new ApiException("Daemon RPC getblock returned no data", HttpStatusCode.BadGateway);
    }

    private async Task<double> GetAverageBlockTimeSecondsAsync(RpcClient rpc, ulong latestHeight, CancellationToken ct)
    {
        if(latestHeight < 2)
            return 0;

        var current = await GetBlockByHeightAsync(rpc, (long) latestHeight, ct);
        var previousHeight = latestHeight > 10 ? (long) latestHeight - 10 : 0;
        var previous = await GetBlockByHeightAsync(rpc, previousHeight, ct);

        var currentTime = current.Value<long?>("time");
        var previousTime = previous.Value<long?>("time");

        if(!currentTime.HasValue || !previousTime.HasValue || currentTime.Value <= previousTime.Value)
            return 0;

        var blocksApart = Math.Max(1, (long) latestHeight - previousHeight);
        return (currentTime.Value - previousTime.Value) / (double) blocksApart;
    }

    private async Task<NetworkTransactionResponse[]> MapTransactionsAsync(RpcClient rpc, JObject block, CancellationToken ct)
    {
        if(block["tx"] is not JArray transactions)
            return Array.Empty<NetworkTransactionResponse>();

        var result = new List<NetworkTransactionResponse>(transactions.Count);

        foreach(var txToken in transactions.OfType<JObject>())
            result.Add(await MapTransactionAsync(rpc, txToken, ct));

        return result.ToArray();
    }

    private async Task<NetworkTransactionResponse> MapTransactionAsync(RpcClient rpc, JObject tx, CancellationToken ct)
    {
        var txId = tx.Value<string>("txid") ?? tx.Value<string>("hash") ?? string.Empty;
        var inputs = new List<NetworkTransactionPoint>();
        var outputs = new List<NetworkTransactionPoint>();
        decimal totalInputs = 0m;
        decimal totalOutputs = 0m;

        if(tx["vin"] is JArray vinArray)
        {
            foreach(var vin in vinArray.OfType<JObject>())
            {
                if(vin["coinbase"] != null)
                    continue;

                var inputPoint = await ResolveInputAsync(rpc, vin, ct);
                inputs.Add(inputPoint);
                totalInputs += inputPoint.Amount;
            }
        }

        if(tx["vout"] is JArray voutArray)
        {
            foreach(var vout in voutArray.OfType<JObject>())
            {
                var outputPoint = ResolveOutput(vout);
                outputs.Add(outputPoint);
                totalOutputs += outputPoint.Amount;
            }
        }

        var fees = tx.Value<decimal?>("fee");
        if(!fees.HasValue && inputs.Any())
            fees = totalInputs - totalOutputs;

        return new NetworkTransactionResponse
        {
            Hash = txId,
            Fees = fees ?? 0m,
            Inputs = inputs.ToArray(),
            Outputs = outputs.ToArray()
        };
    }

    private async Task<NetworkTransactionPoint> ResolveInputAsync(RpcClient rpc, JObject vin, CancellationToken ct)
    {
        var prevout = vin["prevout"] as JObject;

        if(prevout != null)
            return new NetworkTransactionPoint
            {
                Address = ResolveAddress(prevout["scriptPubKey"] as JObject),
                Amount = prevout.Value<decimal?>("value") ?? 0m
            };

        var txid = vin.Value<string>("txid");
        var voutIndex = vin.Value<int?>("vout");

        if(string.IsNullOrEmpty(txid) || !voutIndex.HasValue)
            throw new ApiException("Unable to resolve transaction input", HttpStatusCode.BadGateway);

        var rawTx = await rpc.ExecuteAsync<JObject>(logger, "getrawtransaction", ct, new object[] { txid, true });

        if(rawTx.Error != null)
            throw new ApiException($"Daemon RPC getrawtransaction failed: {rawTx.Error.Message}", HttpStatusCode.BadGateway);

        var previousTx = rawTx.Response ?? throw new ApiException("Daemon RPC getrawtransaction returned no data", HttpStatusCode.BadGateway);
        var previousOutput = previousTx["vout"]?.Children<JObject>().FirstOrDefault(x => x.Value<int?>("n") == voutIndex.Value);

        if(previousOutput == null)
            throw new ApiException($"Unable to resolve transaction input {txid}:{voutIndex.Value}", HttpStatusCode.BadGateway);

        return new NetworkTransactionPoint
        {
            Address = ResolveAddress(previousOutput["scriptPubKey"] as JObject),
            Amount = previousOutput.Value<decimal?>("value") ?? 0m
        };
    }

    private static NetworkTransactionPoint ResolveOutput(JObject vout)
    {
        return new NetworkTransactionPoint
        {
            Address = ResolveAddress(vout["scriptPubKey"] as JObject),
            Amount = vout.Value<decimal?>("value") ?? 0m
        };
    }

    private static string ResolveAddress(JObject scriptPubKey)
    {
        if(scriptPubKey == null)
            return string.Empty;

        var address = scriptPubKey.Value<string>("address");
        if(!string.IsNullOrEmpty(address))
            return address;

        if(scriptPubKey["addresses"] is JArray addresses && addresses.Count > 0)
            return addresses[0]?.Value<string>() ?? string.Empty;

        return string.Empty;
    }

    private static string ToIsoTimestamp(long? unixSeconds)
    {
        if(!unixSeconds.HasValue || unixSeconds.Value <= 0)
            return string.Empty;

        return DateTimeOffset.FromUnixTimeSeconds(unixSeconds.Value).UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
    }

    private static decimal GetCoinbaseReward(JObject block)
    {
        if(block["tx"] is not JArray txs || txs.Count == 0)
            return 0m;

        if(txs[0] is not JObject coinbase)
            return 0m;

        if(coinbase["vout"] is not JArray outputs)
            return 0m;

        return outputs.OfType<JObject>().Sum(x => x.Value<decimal?>("value") ?? 0m);
    }

    private async Task<decimal> GetTotalSupplyAsync(PoolConfig pool, CancellationToken ct)
    {
        var rpc = CreateRpcClient(pool);
        return await ExecuteDecimalAsync(rpc, "gettxoutsetinfo", null, ct, "total_amount");
    }
}
