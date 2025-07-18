using Serilog;
using ZaWarudo.Model;

namespace ZaWarudo.Scheduler;

public class Scheduler : IScheduler
{
    private readonly ILogger _logger = Log.ForContext<Scheduler>();

    private List<Operation> _operations;
    // E_3-r3(X) w3(Y) c1 r1(X) w1(Y) c2 r2(Y) w2(Z) c3

    private string _scheduleId { get; set; } = string.Empty;

    public Dictionary<string, LogRecord> _logRecords { get; set; } = new();

    public Dictionary<string, TransactionRecord> _transaction { get; set; } = new();

    public Task<Result<Unit, SchedulerError>> InitializeAsync()
    {
        _logger.Debug("Initializing scheduler...");

        _operations = [];
        _logRecords = [];
        _transaction = [];

        _logger.Information("Scheduler initialized successfully");
        return Task.FromResult(Result<Unit, SchedulerError>.Success(Unit.Value));
    }

    public Task<Result<Unit, SchedulerError>> SetScheduleAsync(string scheduleId, List<Operation> operations
    )
    {
        _logger.Debug("Setting schedule with ID {ScheduleId} and operations count {OperationsCount}",
            scheduleId, operations.Count);
        _scheduleId = scheduleId;
        _operations = operations;

        _logger.Information("Schedule {ScheduleId} set with {OperationsCount} operations",
            scheduleId, operations.Count);
        return Task.FromResult(Result<Unit, SchedulerError>.Success(Unit.Value));
    }


    public Task<Result<Unit, SchedulerError>> ResetScheduler()
    {
        _logger.Debug("Resetting timestamps for log records...");

        _logRecords.ToList().ForEach(record => record.Value.ResetRecord());
        _operations = [];

        _logger.Information("Timestamps for log records reset successfully");
        return Task.FromResult(Result<Unit, SchedulerError>.Success(Unit.Value));
    }

    public Task<Result<string, SchedulerError>> CheckIfSerializableAsync()
    {
        _logger.Debug("Checking if schedule {ScheduleId} is serializable", _scheduleId);

        foreach (var op in _operations)
        {
            bool result;
            switch (op.OperationType)
            {
                case OperationType.Read:
                    var readResult = CheckIfIsReadable(op.TransactionId, op.DataId);
                    if (readResult.IsError)
                    {
                        _logger.Error("Read operation failed for transaction {TransactionId} and data {DataId}",
                            op.TransactionId, op.DataId);
                        return Task.FromResult(
                            Result<string, SchedulerError>.Error(readResult.GetErrorOrThrow()));
                    }

                    result = readResult.GetValueOrThrow();
                    break;
                case OperationType.Write:
                    var writeResult = CheckIfIsWritable(op.TransactionId, op.DataId);
                    if (writeResult.IsError)
                    {
                        _logger.Error("Write operation failed for transaction {TransactionId} and data {DataId}",
                            op.TransactionId, op.DataId);
                        return Task.FromResult(
                            Result<string, SchedulerError>.Error(writeResult.GetErrorOrThrow()));
                    }

                    result = writeResult.GetValueOrThrow();
                    break;
                case OperationType.Commit:
                default:
                    result = true;
                    break;
            }

            if (result == false)
            {
                // registrar uma marca ̧c ̃ao “ROLLBACK” de re-inicialização de transação, juntamente com o seu “momento” como definido abaixo.
                var schedulerResult = _scheduleId + "-ROLLBACK-" + 0;

                _logger.Information("The schedule {ScheduleId} is not serializable, rolling back", _scheduleId);
                return Task.FromResult(Result<string, SchedulerError>.Success(schedulerResult));
            }
        }

        var scheduleResult = _scheduleId + "-OK";

        _logger.Information("The schedule {ScheduleId} is serializable", _scheduleId);
        return Task.FromResult(Result<string, SchedulerError>.Success(scheduleResult));
    }

    private Result<bool, SchedulerError> CheckIfIsReadable(string transactionId, string logRecordId)
    {
        if (!_transaction.TryGetValue(transactionId, out var transactionRecord))
        {
            _logger.Error("Transaction {TransactionId} not found for read operation", transactionId);
            return Result<bool, SchedulerError>.Error(
                new SchedulerError($"Transaction {transactionId} not found"));
        }

        if (!_logRecords.TryGetValue(logRecordId, out var logRecord))
        {
            _logger.Error("Log Record {logRecord} not found for read operation", logRecordId);
            return Result<bool, SchedulerError>.Error(
                new SchedulerError($"Transaction {transactionId} not found"));
        }


        var isReadable = logRecord.IsReadable(transactionRecord);

        return Result<bool, SchedulerError>.Success(isReadable);
    }


    private Result<bool, SchedulerError> CheckIfIsWritable(string transactionId, string logRecordId)
    {
        if (!_transaction.TryGetValue(transactionId, out var transactionRecord))
        {
            _logger.Error("Transaction {TransactionId} not found for write operation", transactionId);
            return Result<bool, SchedulerError>.Error(
                new SchedulerError($"Transaction {transactionId} not found"));
        }

        if (!_logRecords.TryGetValue(logRecordId, out var logRecord))
        {
            _logger.Error("Log Record {logRecord} not found for write operation", logRecordId);
            return Result<bool, SchedulerError>.Error(
                new SchedulerError($"Transaction {transactionId} not found"));
        }


        var isReadable = logRecord.IsWritable(transactionRecord);

        return Result<bool, SchedulerError>.Success(isReadable);
    }
}
