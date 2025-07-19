using Microsoft.Extensions.Configuration;
using Serilog;
using ZaWarudo.Model;
using ZaWarudo.Parser;
using ZaWarudo.Scheduler;

namespace ZaWarudo.Services;

/// <summary>
///     Factory class that creates application components and configures the application
/// </summary>
public static class ApplicationFactory
{
    /// <summary>
    ///     Configures Serilog based on application settings
    /// </summary>
    public static void ConfigureSerilog()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        Log.Debug("Configured logging for Za-Warudo Timestamp-Based Scheduling Application");
    }

    /// <summary>
    ///     Parses ann load input data from the specified path
    /// </summary>
    /// <param name="inputPath">the file path of input file</param>
    public static Result<InputFileParsed, ParserError> LoadInputData(string inputPath)
    {
        Log.Debug("Loading input data from {inputPath}", inputPath);


        var inputResult = InputParser.ParseInput(inputPath);

        if (inputResult.IsError)
        {
            Log.Error("Failed to parse input data: {Error}", inputResult.GetErrorOrThrow());
        }
        else
        {
            var inputData = inputResult.GetValueOrThrow();
            Log.Information(
                "Input data loaded successfully: {DataRecordsCount} data records, {TransactionsCount} transactions and {SchedulePlansCount} schedule plans",
                inputData.DataRecords.Count, inputData.TransactionRecords.Count, inputData.SchedulePlans.Count);
        }

        return inputResult;
    }

    /// <summary>
    ///     Creates a scheduler instance and initializes it with the provided data records and transaction records
    /// </summary>
    /// <param name="dataRecords">list of data records</param>
    /// <param name="transactionRecords">list of transaction records</param>
    /// <returns>Scheduler interface</returns>
    public static IScheduler CreateScheduler(
        List<DataRecord> dataRecords,
        List<TransactionRecord> transactionRecords)
    {
        Log.Information("Creating scheduler instance");
        var scheduler = new Scheduler.Scheduler();

        scheduler.SetDataRecords(dataRecords.ToDictionary(t => t.Id, t => t));
        scheduler.SetTransaction(transactionRecords.ToDictionary(t => t.Id, t => t));

        return scheduler;
    }

    /// <summary>
    ///     Create a scheduler processor instance that will process the schedule plans
    /// </summary>
    /// <param name="scheduler">scheduler instance</param>
    /// <returns>scheduler processor instance</returns>
    public static ISchedulerProcessor CreateSchedulerProcessor(IScheduler scheduler)
    {
        Log.Information("Creating scheduler processor instance");
        return new ScheduleProcessor(scheduler);
    }
}
