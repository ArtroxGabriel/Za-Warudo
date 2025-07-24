using ZaWarudo.Model;

namespace ZaWarudo.Scheduler;

public interface IScheduler
{
    Task<Result<Unit, SchedulerError>> InitializeAsync();

    Task<Result<Unit, SchedulerError>> SetScheduleAsync(SchedulePlan schedulePlan);

    Result<Unit, SchedulerError> SetDataRecords(Dictionary<string, DataRecord> dataRecords);

    Result<Unit, SchedulerError> SetTransaction(Dictionary<string, TransactionRecord> transaction);

    Task<Result<Unit, SchedulerError>> ResetScheduler();
    Task<Result<string, SchedulerError>> CheckIfSerializableAsync();
    Task<Result<List<DataRecord>, SchedulerError>> GetDataRecords();
    Task<Result<Dictionary<string, IEnumerable<string>>, SchedulerError>> GetOperationsForDataRecords();
}

public readonly struct SchedulerError(string msg)
{
    public string Message { get; } = msg;
}
