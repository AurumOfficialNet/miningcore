namespace Miningcore.Api.Responses;

public class MinerTransaction
{
    public string TxId { get; set; }
    public string Address { get; set; }
    public string AddressInfoLink { get; set; }
    public decimal Amount { get; set; }
    public string Category { get; set; }
    public long BlockHeight { get; set; }
    public string BlockHash { get; set; }
    public string TransactionInfoLink { get; set; }
    public DateTime BlockTime { get; set; }
}
