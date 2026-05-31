using System.Data;
using Miningcore.Persistence.Model;

namespace Miningcore.Persistence.Repositories;

public interface IMinerTransactionRepository
{
    Task BatchInsertAsync(IDbConnection con, IDbTransaction tx, IEnumerable<MinerTransaction> transactions, CancellationToken ct);
    Task<MinerTransaction[]> PageTransactionsAsync(IDbConnection con, string coin, string address, int page, int pageSize, CancellationToken ct);
    Task<uint> GetTransactionCountAsync(IDbConnection con, string coin, string address, CancellationToken ct);
    Task<long> GetLastIndexedHeightAsync(IDbConnection con, string coin, CancellationToken ct);
    Task UpsertIndexerStateAsync(IDbConnection con, IDbTransaction tx, string coin, long height, CancellationToken ct);
}
