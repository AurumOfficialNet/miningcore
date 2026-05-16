namespace Miningcore.Api.Responses;

public class AdminBlocksResponse
{
    public int PendingBlocks { get; set; }
    public int ConfirmedBlocks { get; set; }
    public int OrphanedBlocks { get; set; }
    public int PayoutQueue { get; set; }
    public int PayoutFailures { get; set; }
}
