using Serilog;
using ZaWarudo.Model;

namespace ZaWarudo.Scheduler;

public class Scheduler : IScheduler
{
    private readonly ILogger _logger = Log.ForContext<Scheduler>();

    private List<Operation> _operations = new();

    private string _scheduleId { get; set; } = string.Empty;

    private Dictionary<string, DataRecord> _dataRecords { get; set; } = new();

    private Dictionary<string, TransactionRecord> _transactions { get; set; } = new();

    public Dictionary<string, IEnumerable<string>> OperationsForDataRecord { get; set; } = new();

    public Task<Result<Unit, SchedulerError>> InitializeAsync()
    {
        _logger.Debug("Initializing scheduler...");

        _operations = [];
        _dataRecords = [];
        _transactions = [];

        _logger.Information("Scheduler initialized successfully");
        return Task.FromResult(Result<Unit, SchedulerError>.Success(Unit.Value));
    }

    public Task<Result<Unit, SchedulerError>> SetScheduleAsync(SchedulePlan schedulePlan)
    {
        _logger.Debug(
            "Setting schedule with ID {ScheduleId} and operations count {OperationsCount}",
            schedulePlan.ScheduleId,
            schedulePlan.Operations.Count
        );
        _scheduleId = schedulePlan.ScheduleId;
        _operations = schedulePlan.Operations;

        _logger.Information(
            "Schedule {ScheduleId} set with {OperationsCount} operations",
            schedulePlan.ScheduleId,
            schedulePlan.Operations.Count
        );
        return Task.FromResult(Result<Unit, SchedulerError>.Success(Unit.Value));
    }

    public Task<Result<Unit, SchedulerError>> ResetScheduler()
    {
        _logger.Debug("Resetting timestamps for log records...");

        _dataRecords.ToList().ForEach(record => record.Value.ResetRecord());
        _operations = [];

        _logger.Information("Timestamps for log records reset successfully");
        return Task.FromResult(Result<Unit, SchedulerError>.Success(Unit.Value));
    }

    public Task<Result<string, SchedulerError>> CheckIfSerializableAsync()
    {
        _logger.Debug("Checking if schedule {ScheduleId} is serializable", _scheduleId);

        var scheduleTime = 0u;
        foreach (var op in _operations)
        {
            var result = true;
            switch (op.Type)
            {
                case OperationType.Read:
                    var readResult = CheckIfIsReadable(op.TransactionId, op.DataId);
                    if (readResult.IsError)
                    {
                        _logger.Error(
                            "Read operation failed for transaction {TransactionId} and data {DataId}",
                            op.TransactionId,
                            op.DataId
                        );
                        return Task.FromResult(
                            Result<string, SchedulerError>.Error(readResult.GetErrorOrThrow())
                        );
                    }

                    result = readResult.GetValueOrThrow();

                    break;
                case OperationType.Write:
                    var writeResult = CheckIfIsWritable(op.TransactionId, op.DataId);
                    if (writeResult.IsError)
                    {
                        _logger.Error(
                            "Write operation failed for transaction {TransactionId} and data {DataId}",
                            op.TransactionId,
                            op.DataId
                        );
                        return Task.FromResult(
                            Result<string, SchedulerError>.Error(writeResult.GetErrorOrThrow())
                        );
                    }

                    result = writeResult.GetValueOrThrow();
                    break;
                case OperationType.Commit:
                    foreach (var keyValuePair in _dataRecords) keyValuePair.Value.ResetRecord();

                    break;
                default:
                    result = true;
                    break;
            }

            _logger.Debug("Inserting operation {OperationType} for schedule {ScheduleId} and data {DataId}",
                op.Type,
                _scheduleId,
                op.DataId
            );

            var operationString = $"{_scheduleId},{op.Type.ToString().ToLowerInvariant()},{scheduleTime}";
            if (op.Type != OperationType.Commit)
                OperationsForDataRecord[op.DataId] = OperationsForDataRecord[op.DataId].Append(operationString);

            if (!result)
            {
                var schedulerResult = _scheduleId + "-ROLLBACK-" + scheduleTime;

                _logger.Information(
                    "The schedule {ScheduleId} is not serializable, rolling back at {scheduleTime}",
                    _scheduleId,
                    scheduleTime
                );
                return Task.FromResult(Result<string, SchedulerError>.Success(schedulerResult));
            }

            var updateResult = UpdateRecordTimeStamp(op.Type, op.TransactionId, op.DataId);
            if (updateResult.IsError)
            {
                _logger.Error(
                    "Failed to update timestamp for operation {OperationType} on transaction {TransactionId} and data {DataId}",
                    op.Type,
                    op.TransactionId,
                    op.DataId
                );
                return Task.FromResult(
                    Result<string, SchedulerError>.Error(updateResult.GetErrorOrThrow())
                );
            }

            scheduleTime++;
        }

        var scheduleResult = _scheduleId + "-OK";

        _logger.Information("The schedule {ScheduleId} is serializable", _scheduleId);
        return Task.FromResult(Result<string, SchedulerError>.Success(scheduleResult));
    }

    public Task<Result<List<DataRecord>, SchedulerError>> GetDataRecords()
    {
        var list = _dataRecords.ToList().Select(dr => dr.Value).ToList();

        return Task.FromResult(Result<List<DataRecord>, SchedulerError>.Success(list));
    }

    public Task<Result<Dictionary<string, IEnumerable<string>>, SchedulerError>> GetOperationsForDataRecords()
    {
        return Task.FromResult(Result<Dictionary<string, IEnumerable<string>>, SchedulerError>.Success(
            OperationsForDataRecord));
    }

    public Result<Unit, SchedulerError> SetDataRecords(Dictionary<string, DataRecord> dataRecords)
    {
        _dataRecords = dataRecords;
        foreach (var keyValuePair in _dataRecords) OperationsForDataRecord.Add(keyValuePair.Key, new List<string>());

        return Result<Unit, SchedulerError>.Success(Unit.Value);
    }

    public Result<Unit, SchedulerError> SetTransaction(
        Dictionary<string, TransactionRecord> transaction
    )
    {
        _transactions = transaction;

        return Result<Unit, SchedulerError>.Success(Unit.Value);
    }

    private Result<bool, SchedulerError> CheckIfIsReadable(
        string transactionId,
        string dataRecordId
    )
    {
        if (!_transactions.TryGetValue(transactionId, out var transactionRecord))
        {
            _logger.Error(
                "Transaction {TransactionId} not found for read operation",
                transactionId
            );
            return Result<bool, SchedulerError>.Error(
                new SchedulerError($"Transaction {transactionId} not found")
            );
        }

        if (!_dataRecords.TryGetValue(dataRecordId, out var dataRecord))
        {
            _logger.Error("Log Record {logRecord} not found for read operation", dataRecordId);
            return Result<bool, SchedulerError>.Error(
                new SchedulerError($"Transaction {transactionId} not found")
            );
        }

        var isReadable = dataRecord.IsReadable(transactionRecord);

        return Result<bool, SchedulerError>.Success(isReadable);
    }

    private Result<bool, SchedulerError> CheckIfIsWritable(
        string transactionId,
        string dataRecordId
    )
    {
        if (!_transactions.TryGetValue(transactionId, out var transactionRecord))
        {
            _logger.Error(
                "Transaction {TransactionId} not found for write operation",
                transactionId
            );
            return Result<bool, SchedulerError>.Error(
                new SchedulerError($"Transaction {transactionId} not found")
            );
        }

        if (!_dataRecords.TryGetValue(dataRecordId, out var dataRecord))
        {
            _logger.Error("Log Record {logRecord} not found for write operation", dataRecordId);
            return Result<bool, SchedulerError>.Error(
                new SchedulerError($"Transaction {transactionId} not found")
            );
        }

        var isReadable = dataRecord.IsWritable(transactionRecord);

        return Result<bool, SchedulerError>.Success(isReadable);
    }

    private Result<Unit, SchedulerError> UpdateRecordTimeStamp(
        OperationType operationType,
        string transactionId,
        string dataRecordId
    )
    {
        if (operationType == OperationType.Commit)
        {
            _logger.Debug("No timestamp update needed for commit operation");
            return Result<Unit, SchedulerError>.Success(Unit.Value);
        }

        if (!_transactions.TryGetValue(transactionId, out var transactionRecord))
        {
            _logger.Error(
                "Transaction {TransactionId} not found for write operation",
                transactionId
            );
            return Result<Unit, SchedulerError>.Error(
                new SchedulerError($"Transaction {transactionId} not found")
            );
        }

        if (!_dataRecords.TryGetValue(dataRecordId, out var logRecord))
        {
            _logger.Error("Log Record {logRecord} not found for write operation", dataRecordId);
            return Result<Unit, SchedulerError>.Error(
                new SchedulerError($"Transaction {transactionId} not found")
            );
        }

        switch (operationType)
        {
            case OperationType.Read:
                logRecord.SetTsRead(transactionRecord.Timestamp);
                break;
            case OperationType.Write:
                logRecord.SetTsWrite(transactionRecord.Timestamp);
                break;
        }

        return Result<Unit, SchedulerError>.Success(Unit.Value);
    }
}
