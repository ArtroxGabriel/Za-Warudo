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

    // I'm sorry but the guys said that if we do a commit all timestamps are reseted. I'm sorry Rhaman
    [Theory]
    [InlineData(
        "E_1-r1(A) r4(A) r3(A) r3(B) r2(A) c",
        "E_1-OK"
    )]
    [InlineData(
        "E_2-r1(A) c w4(A) r2(A) r3(C) c",
        "E_2-OK"
    )]
    [InlineData(
        "E_3-w4(B) r1(B) r2(B) c r4(A) r3(A) r3(D) w3(D) r2(D) r2(B) c",
        "E_3-OK"
    )]
    [InlineData(
        "E_4-w4(B) r1(B) r2(B) c r4(A) r3(A) r3(D) w3(D) r4(D) w4(D) r2(C) w1(D) w3(D) c r3(C) r3(B) r2(A) c",
        "E_4-ROLLBACK-12"
    )]
    [InlineData(
        "E_5-w4(B) r1(B) r2(B) c r4(A) r3(A) r3(D) w3(D) r4(D) w4(D) r2(C) w1(D) c w3(D) r3(C) r3(B) r2(A) c",
        "E_5-OK"
    )]
    [InlineData(
        "E_6-r1(A) r2(A) w2(B) w3(C) c w3(B) w4(A) w4(B) c",
        "E_6-OK"
    )]
    [InlineData(
        "E_7-w1(A) r2(B) r1(B) w2(B) r1(A) c w3(B) w4(A) w2(B) c",
        "E_7-OK"
    )]
    [InlineData(
        "E_8-w1(A) r2(B) r1(B) w2(B) r1(A) w3(B) w4(A) w2(B) c",
        "E_8-ROLLBACK-5"
    )]
    [InlineData(
        "E_9-w1(A) r2(B) r1(B) r1(A) w3(B) w4(A) w2(B) c",
        "E_9-ROLLBACK-4"
    )]
    public async Task CheckIfSerializableAsync_WithProvidedSchedules_ShouldReturnExpectedResult(
        string scheduleString,
        string expected
    )
    {
        var dataRecordsString = "A, B, C, D";
        var transactionsString = "t1, t2, t3, t4";
        var timestampsString = "8, 9, 1, 4";
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
        var transactionIds = transactionsString.Split(',').Select(t => t.Trim().ToUpper()).ToArray();
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
            .Select(id => new DataRecord(id.Trim().ToUpper()))
            .ToDictionary(d => d.Id, d => d);
    }

    [GeneratedRegex(@"(r|w|c)(\d*)(?:\(([^)]*)\))?")]
    private static partial Regex OperationRegex();
}
