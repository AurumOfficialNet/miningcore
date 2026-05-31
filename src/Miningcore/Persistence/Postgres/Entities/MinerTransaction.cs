namespace Miningcore.Persistence.Postgres.Entities;

public class MinerTransaction
{
    public long Id { get; set; }
    public string Coin { get; set; }
    public string TxId { get; set; }
    public string Address { get; set; }
    public decimal Amount { get; set; }
    public string Category { get; set; }
    public long BlockHeight { get; set; }
    public string BlockHash { get; set; }
    public DateTime BlockTime { get; set; }
    public DateTime Created { get; set; }
}
