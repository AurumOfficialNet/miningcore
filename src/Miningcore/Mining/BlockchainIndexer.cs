using System.Data;
using System.Data.Common;
using System.Net.Sockets;
using Autofac;
using Dapper;
using Microsoft.Extensions.Hosting;
using Miningcore.Blockchain.Bitcoin;
using Miningcore.Configuration;
using Miningcore.Contracts;
using Miningcore.Extensions;
using Miningcore.Messaging;
using Miningcore.Persistence;
using Miningcore.Persistence.Model;
using Miningcore.Persistence.Repositories;
using Miningcore.Rpc;
using Miningcore.Time;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Polly;

namespace Miningcore.Mining;

public class BlockchainIndexer : BackgroundService
{
    public BlockchainIndexer(
        IComponentContext ctx,
        IMasterClock clock,
        IConnectionFactory cf,
        IMessageBus messageBus,
        ClusterConfig clusterConfig,
        JsonSerializerSettings serializerSettings,
        IMinerTransactionRepository miningTxRepo)
    {
        Contract.RequiresNonNull(ctx);
        Contract.RequiresNonNull(clock);
        Contract.RequiresNonNull(cf);
        Contract.RequiresNonNull(messageBus);
        Contract.RequiresNonNull(clusterConfig);
        Contract.RequiresNonNull(serializerSettings);
        Contract.RequiresNonNull(miningTxRepo);

        this.clock = clock;
        this.cf = cf;
        this.messageBus = messageBus;
        this.clusterConfig = clusterConfig;
        this.serializerSettings = serializerSettings;
        this.miningTxRepo = miningTxRepo;

        BuildFaultHandlingPolicy();
    }

    private readonly IMasterClock clock;
    private readonly IConnectionFactory cf;
    private readonly IMessageBus messageBus;
    private readonly ClusterConfig clusterConfig;
    private readonly JsonSerializerSettings serializerSettings;
    private readonly IMinerTransactionRepository miningTxRepo;
    private IAsyncPolicy rpcFaultPolicy;

    private const int BatchSize = 100;
    private const int RetryCount = 3;
    private static readonly TimeSpan CycleInterval = TimeSpan.FromSeconds(30);

    private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

    private void BuildFaultHandlingPolicy()
    {
        rpcFaultPolicy = Policy
            .Handle<DbException>()
            .Or<SocketException>()
            .Or<TimeoutException>()
            .RetryAsync(RetryCount, (ex, retry) =>
                logger.Warn(() => $"Retry {retry} due to {ex.Source}: {ex.GetType().Name} ({ex.Message})"));
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.Info(() => "Online");

        // warm-up delay
        await Task.Delay(TimeSpan.FromSeconds(20), ct);

        while(!ct.IsCancellationRequested)
        {
            try
            {
                var eligiblePools = clusterConfig.Pools
                    .Where(p => p.Enabled && p.Daemons?.Length > 0)
                    .Where(p => p.Template?.Family == CoinFamily.Bitcoin || p.Template?.Family == CoinFamily.Equihash)
                    .ToArray();

                var indexJobs = eligiblePools
                    .Select(pool => new { Pool = pool, Coin = ResolveCoin(pool) })
                    .Where(x => !string.IsNullOrEmpty(x.Coin))
                    .GroupBy(x => x.Coin)
                    .Select(g => g.First())
                    .ToArray();

                var skippedPools = clusterConfig.Pools
                    .Where(p => p.Enabled && p.Daemons?.Length > 0)
                    .Where(p => p.Template?.Family != CoinFamily.Bitcoin && p.Template?.Family != CoinFamily.Equihash)
                    .ToArray();

                foreach(var pool in skippedPools)
                    logger.Warn(() => $"[{pool.Id}] Skipping blockchain indexer: coin family '{pool.Template?.Family}' is not supported (only Bitcoin and Equihash families are supported)");

                await Task.WhenAll(indexJobs.Select(job => IndexCoinAsync(job.Pool, job.Coin, ct)));
            }
            catch(OperationCanceledException)
            {
                break;
            }
            catch(Exception ex)
            {
                logger.Error(ex, "Unexpected error in blockchain indexer loop");
            }

            await Task.Delay(CycleInterval, ct).ContinueWith(_ => { }, CancellationToken.None);
        }

        logger.Info(() => "Offline");
    }

    private async Task IndexCoinAsync(PoolConfig pool, string coin, CancellationToken ct)
    {
        try
        {
            var rpc = new RpcClient(pool.Daemons.First(), serializerSettings, messageBus, pool.Id);

            // get current chain tip
            var blockchainInfoResult = await rpc.ExecuteAsync<JObject>(logger, BitcoinCommands.GetBlockchainInfo, ct, null);
            if(blockchainInfoResult.Error != null)
            {
                logger.Error(() => $"[{coin}] getblockchaininfo failed: {blockchainInfoResult.Error.Message}");
                return;
            }

            var currentHeight = blockchainInfoResult.Response?.Value<long?>("blocks") ?? 0;
            if(currentHeight <= 0)
            {
                logger.Warn(() => $"[{coin}] Chain height is 0 or unavailable, skipping");
                return;
            }

            // get last indexed height from DB
            var lastIndexedHeight = await cf.Run(con => miningTxRepo.GetLastIndexedHeightAsync(con, coin, ct));

            if(lastIndexedHeight >= currentHeight)
            {
                logger.Debug(() => $"[{coin}] Already up to date at height {lastIndexedHeight}");
                return;
            }

            var startHeight = lastIndexedHeight + 1;
            logger.Info(() => $"[{coin}] Indexing blocks {startHeight} to {currentHeight}");

            for(var batchStart = startHeight; batchStart <= currentHeight && !ct.IsCancellationRequested; batchStart += BatchSize)
            {
                var batchEnd = Math.Min(batchStart + BatchSize - 1, currentHeight);
                var transactions = new List<MinerTransaction>();

                for(var height = batchStart; height <= batchEnd && !ct.IsCancellationRequested; height++)
                {
                    await rpcFaultPolicy.ExecuteAsync(async () =>
                    {
                        var block = await GetBlockByHeightAsync(rpc, coin, height, ct);
                        if(block == null)
                            return;

                        var blockHash = block.Value<string>("hash") ?? string.Empty;
                        var blockTimeUnix = block.Value<long?>("time") ?? 0;
                        var blockTime = blockTimeUnix > 0
                            ? DateTimeOffset.FromUnixTimeSeconds(blockTimeUnix).UtcDateTime
                            : clock.Now;
                        var now = clock.Now;

                        if(block["tx"] is not JArray txArray)
                            return;

                        foreach(var txToken in txArray.OfType<JObject>())
                        {
                            var txId = txToken.Value<string>("txid") ?? txToken.Value<string>("hash") ?? string.Empty;
                            if(string.IsNullOrEmpty(txId))
                                continue;

                            var isCoinbase = txToken["vin"] is JArray vin && vin.Count > 0 &&
                                             vin[0] is JObject firstVin && firstVin["coinbase"] != null;
                            var category = isCoinbase ? "coinbase" : "payment";

                            if(txToken["vout"] is not JArray voutArray)
                                continue;

                            foreach(var vout in voutArray.OfType<JObject>())
                            {
                                var address = ResolveAddress(vout["scriptPubKey"] as JObject);
                                if(string.IsNullOrEmpty(address))
                                    continue;

                                var amount = vout.Value<decimal?>("value") ?? 0m;

                                transactions.Add(new MinerTransaction
                                {
                                    Coin = coin,
                                    TxId = txId,
                                    Address = address,
                                    Amount = amount,
                                    Category = category,
                                    BlockHeight = height,
                                    BlockHash = blockHash,
                                    BlockTime = blockTime,
                                    Created = now
                                });
                            }
                        }
                    });
                }

                if(transactions.Count > 0 || batchEnd > lastIndexedHeight)
                {
                    // Persist batch using ON CONFLICT DO NOTHING for idempotency
                    await cf.RunTx(async (con, tx) =>
                    {
                        if(transactions.Count > 0)
                        {
                            // COPY doesn't support ON CONFLICT, so use individual inserts for conflict safety
                            await InsertTransactionsAsync(con, tx, transactions, ct);
                        }

                        await miningTxRepo.UpsertIndexerStateAsync(con, tx, coin, batchEnd, ct);
                    });

                    logger.Debug(() => $"[{coin}] Indexed blocks {batchStart}-{batchEnd}, {transactions.Count} tx outputs recorded");
                }
            }

            logger.Info(() => $"[{coin}] Indexing complete up to height {currentHeight}");
        }
        catch(OperationCanceledException)
        {
            // ignore
        }
        catch(Exception ex)
        {
            logger.Error(ex, $"[{coin}] Blockchain indexer failed");
        }
    }

    private async Task InsertTransactionsAsync(IDbConnection con, IDbTransaction tx, List<MinerTransaction> transactions, CancellationToken ct)
    {
        // Use parameterized bulk insert with ON CONFLICT DO NOTHING for idempotency
        // (COPY doesn't support ON CONFLICT, so we use a batch INSERT)
        const int chunkSize = 500;

        for(var i = 0; i < transactions.Count; i += chunkSize)
        {
            if(ct.IsCancellationRequested)
                return;

            var chunk = transactions.Skip(i).Take(chunkSize).ToList();
            var rows = chunk.Select((t, idx) => new
            {
                coin = t.Coin,
                txid = t.TxId,
                address = t.Address,
                amount = t.Amount,
                category = t.Category,
                blockheight = t.BlockHeight,
                blockhash = t.BlockHash,
                blocktime = t.BlockTime,
                created = t.Created
            }).ToList();

            const string query = @"INSERT INTO miner_transactions (coin, txid, address, amount, category, blockheight, blockhash, blocktime, created)
                VALUES (@coin, @txid, @address, @amount, @category, @blockheight, @blockhash, @blocktime, @created)
                ON CONFLICT (coin, txid, address) DO NOTHING";

            await con.ExecuteAsync(new CommandDefinition(query, rows, transaction: tx, cancellationToken: ct));
        }
    }

    private async Task<JObject> GetBlockByHeightAsync(RpcClient rpc, string poolId, long height, CancellationToken ct)
    {
        var hashResult = await rpc.ExecuteAsync<string>(logger, "getblockhash", ct, new object[] { height });

        if(hashResult.Error != null)
        {
            logger.Warn(() => $"[{poolId}] getblockhash({height}) failed: {hashResult.Error.Message}");
            return null;
        }

        var blockHash = hashResult.Response;
        if(string.IsNullOrEmpty(blockHash))
        {
            logger.Warn(() => $"[{poolId}] getblockhash({height}) returned empty hash");
            return null;
        }

        var blockResult = await rpc.ExecuteAsync<JObject>(logger, BitcoinCommands.GetBlock, ct, new object[] { blockHash, 2 });

        if(blockResult.Error != null)
        {
            logger.Warn(() => $"[{poolId}] getblock({blockHash}) failed: {blockResult.Error.Message}");
            return null;
        }

        return blockResult.Response;
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

    private static string ResolveCoin(PoolConfig pool)
    {
        if(!string.IsNullOrEmpty(pool.Coin))
            return pool.Coin.ToLowerInvariant();

        if(!string.IsNullOrEmpty(pool.Template?.Symbol))
            return pool.Template.Symbol.ToLowerInvariant();

        return string.Empty;
    }
}
