using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Serilog;
using ZaWarudo.Model;

namespace ZaWarudo.Tests.Integration.Scheduler;

[TestSubject(typeof(ZaWarudo.Scheduler.Scheduler))]
public partial class CheckIfSerializableTests
{
    public CheckIfSerializableTests()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();
    }

    [Theory]
    [InlineData(
        "X, Y",
        "T1, T2, T3",
        "5, 10, 3",
        "E_1-r1(X) r2(Y) w2(Y) r3(Y) w1(X) c1",
        "E_1-ROLLBACK-3"
    )]
    [InlineData(
        "X, Y, Z",
        "T1, T2, T3",
        "5, 10, 3",
        "E_2-w2(X) r1(Y) w3(X) r2(Z) w1(Z) c1",
        "E_2-ROLLBACK-2"
    )]
    [InlineData(
        "X, Y, Z",
        "T1, T2, T3",
        "5, 10, 3",
        "E_3-r3(X) w3(Y) c1 r1(X) w1(Y) c2 r2(Y) w2(Z) c3",
        "E_3-OK"
    )]
    public async Task CheckIfSerializableAsync_WithProvidedSchedules_ShouldReturnExpectedResult(
        string dataRecordsString,
        string transactionsString,
        string timestampsString,
        string scheduleString,
        string expected
    )
    {
        // Arrange
        var scheduler = new ZaWarudo.Scheduler.Scheduler();
        var schedulePlan = ParseSchedule(scheduleString);
        var transactions = CreateTransactionRecords(transactionsString, timestampsString);
        var dataRecords = CreateDataRecords(dataRecordsString);

        scheduler.SetTransaction(transactions);
        scheduler.SetDataRecords(dataRecords);
        await scheduler.SetScheduleAsync(schedulePlan);

        // Act
        var result = await scheduler.CheckIfSerializableAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.GetValueOrThrow());

        // log the data records for debugging
        Log.Debug(
            "Data Records: {DataRecords}",
            string.Join(", ", dataRecords.Values.Select(d => d.ToString()))
        );
    }

    private static SchedulePlan ParseSchedule(string scheduleString)
    {
        var parts = scheduleString.Split('-', 2);
        var scheduleId = parts[0];
        var opsString = parts[1];

        var operations = new List<Operation>();
        var regex = OperationRegex();
        var matches = regex.Matches(opsString);

        foreach (Match match in matches)
        {
            var type = match.Groups[1].Value switch
            {
                "r" => OperationType.Read,
                "w" => OperationType.Write,
                "c" => OperationType.Commit,
                _ => throw new ArgumentException("Invalid operation type")
            };
            var transactionId = $"T{match.Groups[2].Value}";
            var dataId =
                match.Groups.Count > 3 && !string.IsNullOrEmpty(match.Groups[3].Value)
                    ? match.Groups[3].Value
                    : string.Empty;

            operations.Add(new Operation(type, transactionId, dataId));
        }

        return new SchedulePlan(scheduleId, operations);
    }

    private static Dictionary<string, TransactionRecord> CreateTransactionRecords(
        string transactionsString,
        string timestampsString
    )
    {
        var transactionIds = transactionsString.Split(',').Select(t => t.Trim()).ToArray();
        var timestamps = timestampsString.Split(',').Select(t => uint.Parse(t.Trim())).ToArray();

        if (transactionIds.Length != timestamps.Length)
            throw new ArgumentException("Transaction and timestamp counts do not match.");

        return transactionIds
            .Zip(timestamps, (id, ts) => new TransactionRecord(id, ts))
            .ToDictionary(t => t.Id, t => t);
    }

    private static Dictionary<string, DataRecord> CreateDataRecords(string dataRecordsString)
    {
        return dataRecordsString
            .Split(',')
            .Select(id => new DataRecord(id.Trim()))
            .ToDictionary(d => d.Id, d => d);
    }

    [GeneratedRegex(@"(r|w|c)(\d+)(?:\(([^)]*)\))?")]
    private static partial Regex OperationRegex();
}
