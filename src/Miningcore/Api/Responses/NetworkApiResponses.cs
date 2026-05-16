namespace Miningcore.Api.Responses;

public class NetworkSeriesPoint
{
    public string Timestamp { get; set; }
    public double Value { get; set; }
}

public class NetworkOverviewResponse
{
    public double NetworkHashrate { get; set; }
    public double NetworkDifficulty { get; set; }
    public ulong BlockHeight { get; set; }
    public double BlockTimeSeconds { get; set; }
    public decimal TotalSupply { get; set; }
    public NetworkSeriesPoint[] HashrateSeries { get; set; }
    public NetworkSeriesPoint[] DifficultySeries { get; set; }
}

public class NetworkBlockSummaryResponse
{
    public ulong Height { get; set; }
    public string Hash { get; set; }
    public string Timestamp { get; set; }
    public int TxCount { get; set; }
    public decimal Reward { get; set; }
}

public class NetworkTransactionPoint
{
    public string Address { get; set; }
    public decimal Amount { get; set; }
}

public class NetworkTransactionResponse
{
    public string Hash { get; set; }
    public decimal Fees { get; set; }
    public NetworkTransactionPoint[] Inputs { get; set; }
    public NetworkTransactionPoint[] Outputs { get; set; }
}

public class NetworkBlockDetailResponse
{
    public ulong Height { get; set; }
    public string Hash { get; set; }
    public string PrevHash { get; set; }
    public string Timestamp { get; set; }
    public double Difficulty { get; set; }
    public decimal Reward { get; set; }
    public NetworkTransactionResponse[] Transactions { get; set; }
}

public class NetworkTopHolderResponse
{
    public int Rank { get; set; }
    public string Address { get; set; }
    public decimal Balance { get; set; }
    public decimal PercentOfSupply { get; set; }
}