using System.Text.RegularExpressions;
using Serilog;
using ZaWarudo.Model;

namespace ZaWarudo.Parser;

public static partial class InputParser
{
    public static Result<InputFileParsed, ParserError> ParseInput(string filePath)
    {
        Log.Debug("Parsing input data from {InputPath}", filePath);

        if (!File.Exists(filePath))
        {
            Log.Warning("The input file not found: {FilePath}", filePath);
            return Result<InputFileParsed, ParserError>.Error(
                new ParserError($"Input file not found: {filePath}"));
        }

        try
        {
            using var fileReader = new StreamReader(filePath);
            var result = ParseInputReader(fileReader);
            Log.Debug("Input file parsing completed successfully.");
            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to read input file: {FilePath}", filePath);
            return Result<InputFileParsed, ParserError>.Error(
                new ParserError($"Failed to read input file: {ex.Message}"));
        }
    }

    public static Result<InputFileParsed, ParserError> ParseInputReader(TextReader reader)
    {
        Log.Debug("Parsing input data from TextReader");

        try
        {
            var dataRecordsLine = reader.ReadLine();
            if (dataRecordsLine == null)
            {
                Log.Warning("CSV input is empty");
                return Result<InputFileParsed, ParserError>.Error(
                    new ParserError("CSV input is empty"));
            }

            var parseRecordsResult = ParseDataRecords(dataRecordsLine);
            if (parseRecordsResult.IsError)
            {
                Log.Error("Failed to parse data records: {Error}", parseRecordsResult.GetErrorOrThrow());
                return Result<InputFileParsed, ParserError>.Error(parseRecordsResult.GetErrorOrThrow());
            }

            Log.Debug("Parsed data records successfully");
            var dataRecords = parseRecordsResult.GetValueOrThrow();


            var transactionsLine = reader.ReadLine();
            if (transactionsLine == null)
            {
                Log.Warning("No transaction records found in input");
                return Result<InputFileParsed, ParserError>.Error(
                    new ParserError("No transaction records found"));
            }

            var transactionTimestampsLine = reader.ReadLine();
            if (transactionTimestampsLine == null)
            {
                Log.Warning("No transaction timestamps found in input");
                return Result<InputFileParsed, ParserError>.Error(
                    new ParserError("No transaction timestamps found"));
            }

            var transactionRecordsResult =
                ParseTransactionRecords(transactionsLine, transactionTimestampsLine);
            if (transactionRecordsResult.IsError)
            {
                Log.Error("Failed to parse transaction records: {Error}",
                    transactionRecordsResult.GetErrorOrThrow());
                return Result<InputFileParsed, ParserError>.Error(transactionRecordsResult.GetErrorOrThrow());
            }

            Log.Debug("Parsed transaction records successfully");
            var transactions = transactionRecordsResult.GetValueOrThrow();

            var schedulePlans = new List<SchedulePlan>();
            var lineNumber = 4;
            while (reader.ReadLine() is { } line)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    Log.Debug("Skipping empty line at {LineNumber}", lineNumber);
                    continue;
                }

                Log.Debug("Parsing line {LineNumber}: {Line}", lineNumber, line);

                var schedulePlanResult = ParseSchedulePlan(line, lineNumber);
                if (schedulePlanResult.IsError)
                {
                    Log.Error("Failed to parse schedule plan: {Error}", schedulePlanResult.GetErrorOrThrow());
                    return Result<InputFileParsed, ParserError>.Error(schedulePlanResult.GetErrorOrThrow());
                }

                Log.Debug("Parsed schedule plan successfully");

                var schedulePlan = schedulePlanResult.GetValueOrThrow();
                schedulePlans.Add(schedulePlan);

                lineNumber++;
            }

            return Result<InputFileParsed, ParserError>.Success(
                new InputFileParsed(dataRecords, transactions, schedulePlans));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while parsing input data");
            return Result<InputFileParsed, ParserError>.Error(
                new ParserError($"Error while parsing input data: {ex.Message}"));
        }
    }

    public static Result<List<DataRecord>, ParserError> ParseDataRecords(string line)
    {
        Log.Debug("Parsing data records from line: {Line}", line);

        var records = line.TrimEnd(';').Split(',').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c)).ToList();
        if (records.Count == 0)
        {
            Log.Warning("No data records found in line: {Line}", line);
            return Result<List<DataRecord>, ParserError>.Error(
                new ParserError("No data records found", 1));
        }

        var dataRecords = records.Select(record => new DataRecord(record)).ToList();

        Log.Information("Parsed {Count} data records", dataRecords.Count);
        return Result<List<DataRecord>, ParserError>.Success(dataRecords);
    }

    public static Result<List<TransactionRecord>, ParserError> ParseTransactionRecords(string transactionLine,
        string timestampsLine)
    {
        Log.Debug("Parsing transaction records from lines: {TransactionLine}, {TimestampsLine}", transactionLine,
            timestampsLine);

        var transactionIds = transactionLine.TrimEnd(';').Split(',').Select(c => c.Trim().ToUpper())
            .Where(c => !string.IsNullOrEmpty(c)).ToList();
        if (transactionIds.Count == 0)
        {
            Log.Warning("No transaction records found in line: {TransactionLine}", transactionLine);
            return Result<List<TransactionRecord>, ParserError>.Error(
                new ParserError("No transaction records found", 2));
        }

        var timestamps = timestampsLine.TrimEnd(';').Split(',').Select(c => c.Trim())
            .Where(c => !string.IsNullOrEmpty(c)).ToList();
        if (timestamps.Count == 0)
        {
            Log.Warning("No timestamps found in line: {TimestampsLine}", timestampsLine);
            return Result<List<TransactionRecord>, ParserError>.Error(
                new ParserError("No timestamps found", 3));
        }


        if (transactionIds.Count != timestamps.Count)
        {
            Log.Error("Transaction and timestamp counts do not match");
            return Result<List<TransactionRecord>, ParserError>.Error(
                new ParserError("Transaction and timestamp counts do not match", 2));
        }

        try
        {
            var transactions = transactionIds.Zip(timestamps, (id, ts) => new TransactionRecord(id, uint.Parse(ts)))
                .ToList();


            Log.Information("Parsed {Count} transaction records", transactions.Count);
            return Result<List<TransactionRecord>, ParserError>.Success(transactions);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to parse transaction records from lines: {TransactionLine}, {TimestampsLine}",
                transactionLine, timestampsLine);
            return Result<List<TransactionRecord>, ParserError>.Error(
                new ParserError($"Failed to parse transaction records: {ex.Message}", 3)
            );
        }
    }

    public static Result<SchedulePlan, ParserError> ParseSchedulePlan(string scheduleLine, int lineNumber)
    {
        Log.Debug("Parsing schedule plan from Line {LineNumber}: {ScheduleLine}", lineNumber, scheduleLine);

        var parts = scheduleLine.Split("-", 2);
        if (parts.Length != 2)
        {
            Log.Warning("Invalid schedule plan");
            return Result<SchedulePlan, ParserError>.Error(
                new ParserError("Invalid schedule plan format. Expected 'ScheduleId - Operations'", lineNumber));
        }

        var scheduleId = parts[0].Trim();
        var opsString = parts[1].Trim();


        var operations = new List<Operation>();
        var regex = ScheduleRegex();
        var matches = regex.Matches(opsString);
        if (matches.Count == 0)
        {
            Log.Warning("No operations or invalid ", opsString);
            return Result<SchedulePlan, ParserError>.Error(
                new ParserError("No operations found in schedule plan", lineNumber));
        }

        try
        {
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
                var dataId = match.Groups.Count > 3 && !string.IsNullOrEmpty(match.Groups[3].Value)
                    ? match.Groups[3].Value
                    : string.Empty;

                operations.Add(new Operation(type, transactionId, dataId));
            }

            var schedulePlan = new SchedulePlan(scheduleId, operations);

            return Result<SchedulePlan, ParserError>.Success(schedulePlan);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to parse schedule plan from line: {Line}", scheduleLine);
            return Result<SchedulePlan, ParserError>.Error(
                new ParserError($"Failed to parse schedule plan: {ex.Message}", lineNumber));
        }
    }


    [GeneratedRegex(@"([A-Za-z])(\d+)(?:\(([^)]*)\))?")]
    private static partial Regex ScheduleRegex();
}

public readonly struct ParserError(string message, int lineNumber = 0)
{
    private string Message { get; } = message;
    private int LineNumber { get; } = lineNumber;

    public override string ToString()
    {
        return $"Line {LineNumber}: {Message}";
    }
}

public readonly struct InputFileParsed(
    List<DataRecord> dataRecords,
    List<TransactionRecord> transactionRecords,
    List<SchedulePlan> schedulePlans)
{
    public List<DataRecord> DataRecords { get; } = dataRecords;
    public List<TransactionRecord> TransactionRecords { get; } = transactionRecords;
    public List<SchedulePlan> SchedulePlans { get; } = schedulePlans;
}
