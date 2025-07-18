using ZaWarudo.Model;

namespace ZaWarudo.Scheduler;

public interface IScheduler
{
    Task<Result<Unit, SchedulerError>> InitializeAsync();

    Task<Result<Unit, SchedulerError>> SetScheduleAsync(string scheduleId, List<Operation> operations);

    Task<Result<Unit, SchedulerError>> SetDataRecordsAsync(Dictionary<string, DataRecord> dataRecords);

    Task<Result<Unit, SchedulerError>> SetTransactionAsync(Dictionary<string, TransactionRecord> transaction);

    Task<Result<Unit, SchedulerError>> ResetScheduler();
    Task<Result<string, SchedulerError>> CheckIfSerializableAsync();
    Task<Result<List<DataRecord>, SchedulerError>> GetDataRecords();
}

public readonly struct SchedulerError(string msg)
{
    public string Message { get; } = msg;
}
