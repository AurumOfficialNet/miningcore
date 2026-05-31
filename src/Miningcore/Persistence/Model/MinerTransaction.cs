namespace Miningcore.Persistence.Model;

public record MinerTransaction
{
    public long Id { get; init; }
    public string Coin { get; init; }
    public string TxId { get; init; }
    public string Address { get; init; }
    public decimal Amount { get; init; }
    public string Category { get; init; }
    public long BlockHeight { get; init; }
    public string BlockHash { get; init; }
    public DateTime BlockTime { get; init; }
    public DateTime Created { get; init; }
}
