namespace Miningcore.Payments.Abstractions;

public interface IPayoutSchedulerState
{
    DateTime? NextRun { get; }
    void SetNextRun(DateTime? nextRun);
}