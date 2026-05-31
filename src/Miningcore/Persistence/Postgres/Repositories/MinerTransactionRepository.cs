using System.Data;
using AutoMapper;
using Dapper;
using Miningcore.Persistence.Model;
using Miningcore.Persistence.Repositories;
using Npgsql;
using NpgsqlTypes;

namespace Miningcore.Persistence.Postgres.Repositories;

public class MinerTransactionRepository : IMinerTransactionRepository
{
    public MinerTransactionRepository(IMapper mapper)
    {
        this.mapper = mapper;
    }

    private readonly IMapper mapper;

    public async Task BatchInsertAsync(IDbConnection con, IDbTransaction tx, IEnumerable<MinerTransaction> transactions, CancellationToken ct)
    {
        var pgCon = (NpgsqlConnection) con;

        const string query = @"COPY miner_transactions (coin, txid, address, amount, category, blockheight, blockhash, blocktime, created) FROM STDIN (FORMAT BINARY)";

        await using(var writer = await pgCon.BeginBinaryImportAsync(query))
        {
            foreach(var item in transactions)
            {
                await writer.StartRowAsync();

                await writer.WriteAsync(item.Coin);
                await writer.WriteAsync(item.TxId);
                await writer.WriteAsync(item.Address);
                await writer.WriteAsync(item.Amount, NpgsqlDbType.Numeric);
                await writer.WriteAsync(item.Category);
                await writer.WriteAsync(item.BlockHeight, NpgsqlDbType.Bigint);
                await writer.WriteAsync(item.BlockHash);
                await writer.WriteAsync(item.BlockTime, NpgsqlDbType.TimestampTz);
                await writer.WriteAsync(item.Created, NpgsqlDbType.TimestampTz);
            }

            await writer.CompleteAsync();
        }
    }

    public async Task<MinerTransaction[]> PageTransactionsAsync(IDbConnection con, string coin, string address, int page, int pageSize, CancellationToken ct)
    {
        const string query = @"SELECT * FROM miner_transactions
            WHERE coin = @coin AND address = @address
            ORDER BY blocktime DESC OFFSET @offset FETCH NEXT @pageSize ROWS ONLY";

        return (await con.QueryAsync<Entities.MinerTransaction>(new CommandDefinition(query,
                new { coin, address, offset = page * pageSize, pageSize }, cancellationToken: ct)))
            .Select(mapper.Map<MinerTransaction>)
            .ToArray();
    }

    public Task<uint> GetTransactionCountAsync(IDbConnection con, string coin, string address, CancellationToken ct)
    {
        const string query = "SELECT COUNT(*) FROM miner_transactions WHERE coin = @coin AND address = @address";

        return con.ExecuteScalarAsync<uint>(new CommandDefinition(query,
            new { coin, address }, cancellationToken: ct));
    }

    public Task<long> GetLastIndexedHeightAsync(IDbConnection con, string coin, CancellationToken ct)
    {
        const string query = "SELECT COALESCE(lastindexedheight, 0) FROM indexer_state WHERE coin = @coin";

        return con.ExecuteScalarAsync<long>(new CommandDefinition(query,
            new { coin }, cancellationToken: ct));
    }

    public Task UpsertIndexerStateAsync(IDbConnection con, IDbTransaction tx, string coin, long height, CancellationToken ct)
    {
        const string query = @"INSERT INTO indexer_state(coin, lastindexedheight, updated)
            VALUES(@coin, @height, @now)
            ON CONFLICT (coin) DO UPDATE SET lastindexedheight = @height, updated = @now";

        return con.ExecuteAsync(new CommandDefinition(query,
            new { coin, height, now = DateTime.UtcNow }, transaction: tx, cancellationToken: ct));
    }
}
