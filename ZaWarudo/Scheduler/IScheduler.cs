using ZaWarudo.Model;

namespace ZaWarudo.Scheduler;

public interface IScheduler
{
    Task<Result<Unit, SchedulerError>> InitializeAsync();

    Task<Result<Unit, SchedulerError>> SetScheduleAsync(string scheduleid, List<Operation> operations);

    Task<Result<Unit, SchedulerError>> ResetScheduler();
    Task<Result<string, SchedulerError>> CheckIfSerializableAsync();
}

public readonly struct SchedulerError(string msg)
{
    public string Message { get; } = msg;
}
