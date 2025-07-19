using System.CommandLine;
using Serilog;
using ZaWarudo.Services;

namespace ZaWarudo;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        ApplicationFactory.ConfigureSerilog();
        Log.Information("Starting Za-Warudo Timestamp-Based Scheduling Application");

        try
        {
            // args
            var rootCommand = new RootCommand("Za-Warudo Timestamp-Based Scheduling Application");

            var inputOption = new Option<string>(
                "--input",
                "Path to the input file containing data records and transaction records")
            {
                Arity = ArgumentArity.ZeroOrOne
            };
            inputOption.SetDefaultValue("ZaWarudo/Data/in.txt");
            rootCommand.AddOption(inputOption);

            var outputOption = new Option<string>(
                "--output",
                "Path to the output file where results will be saved") { Arity = ArgumentArity.ZeroOrOne };
            outputOption.SetDefaultValue("ZaWarudo/Data/out.txt");
            rootCommand.AddOption(outputOption);

            rootCommand.SetHandler(async context =>
            {
                var inputPath = context.ParseResult.GetValueForOption(inputOption);
                var outputPath = context.ParseResult.GetValueForOption(outputOption);

                Log.Debug("Input file: {InputPath}, Output file: {OutputPath}", inputPath, outputPath);

                var exitCode = await RunApplicationAsync(inputPath, outputPath);
                context.ExitCode = exitCode;
            });

            return await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static async Task<int> RunApplicationAsync(string? inputPath, string? outputPath)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            Log.Error("Input file path is not specified or is empty");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            Log.Error("Output file path is not specified or is empty");
            return 1;
        }

        try
        {
            var inputResult = ApplicationFactory.LoadInputData(inputPath);
            if (inputResult.IsError)
            {
                Log.Error("Application failed to start due to input parsing error: {Error}",
                    inputResult.GetErrorOrThrow());
                return 1;
            }

            var inputData = inputResult.GetValueOrThrow();
            var scheduler = ApplicationFactory.CreateScheduler(inputData.DataRecords, inputData.TransactionRecords);

            var scheduleProcessor = ApplicationFactory.CreateSchedulerProcessor(scheduler);

            var result = await scheduleProcessor.ProcessScheduleAsync(inputData.SchedulePlans, outputPath);
            if (result.IsError)
            {
                Log.Error("Application failed to process schedule: {Error}", result.GetErrorOrThrow());
                return 1;
            }

            Log.Information("Za-Warudo application completed successfully");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "An unhandled exception occurred in the Za-Warudo application");
            return 1;
        }
    }
}
