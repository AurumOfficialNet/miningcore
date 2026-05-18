using Miningcore.Payments.Abstractions;

namespace Miningcore.Payments;

public class PayoutSchedulerState : IPayoutSchedulerState
{
    private DateTime? nextRun;
    private readonly object gate = new();

    public DateTime? NextRun
    {
        get
        {
            lock(gate)
                return nextRun;
        }
    }

    public void SetNextRun(DateTime? nextRun)
    {
        lock(gate)
            this.nextRun = nextRun;
    }
}