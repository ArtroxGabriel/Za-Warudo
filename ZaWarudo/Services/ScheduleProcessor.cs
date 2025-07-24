using Serilog;
using ZaWarudo.Model;
using ZaWarudo.Scheduler;

namespace ZaWarudo.Services;

public class ScheduleProcessor(IScheduler scheduler) : ISchedulerProcessor
{
    private readonly ILogger _logger = Log.ForContext<ScheduleProcessor>();

    public async Task<Result<Unit, ProcessorError>> ProcessScheduleAsync(IEnumerable<SchedulePlan> schedulePlans,
        string outputPath)
    {
        _logger.Debug("Processing schedule plans...");

        // Ensure the output directory exists
        if (!Directory.Exists(outputPath))
        {
            _logger.Warning("Output directory does not exist, creating: {OutputDirectory}", outputPath);
            try
            {
                Directory.CreateDirectory(outputPath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to create output directory: {OutputDirectory}", outputPath);
                return Result<Unit, ProcessorError>.Error(
                    new ProcessorError($"Failed to create output directory: {ex.Message}"));
            }
        }

        _logger.Information("Output file will be created at {OutputPath}", outputPath);

        var outputFile = Path.Combine(outputPath, "out.txt");
        await using var fileStream = File.Open(outputFile, FileMode.Create);
        await using var writer = new StreamWriter(fileStream);

        var schedulePlansArray = schedulePlans.ToArray();
        var hasProcessedPlans = false;

        foreach (var plan in schedulePlansArray)
        {
            _logger.Debug("Processing schedule plan with ID {ScheduleId}", plan.ScheduleId);

            var processResult = await ProcessExecutionPlanAsync(plan);
            if (processResult.IsError)
            {
                _logger.Error("Failed to process schedule plan {ScheduleId}: {Error}",
                    plan.ScheduleId, processResult.GetErrorOrThrow());
                return Result<Unit, ProcessorError>.Error(processResult.GetErrorOrThrow());
            }

            var (outputString, resetFailed) = processResult.GetValueOrThrow();
            await writer.WriteLineAsync(outputString);

            // If reset failed, return error after writing the output
            if (resetFailed)
                return Result<Unit, ProcessorError>.Error(
                    new ProcessorError($"Failed to reset scheduler after processing plan {plan.ScheduleId}"));

            hasProcessedPlans = true;
        }

        // Only get operations if we actually processed some plans
        if (hasProcessedPlans)
        {
            _logger.Information("Saving data operations");

            var operationsResult = await scheduler.GetOperationsForDataRecords();
            var operations = operationsResult.GetValueOrThrow();

            foreach (var operation in operations)
            {
                _logger.Debug("Data ID: {DataId}, Operations: {Operations}",
                    operation.Key, string.Join(", ", operation.Value));

                // For data operations, use the same directory as the main output file
                var dataOperationFilePath = Path.Combine(outputPath, $"{operation.Key}.txt");

                await using var dataOperationFileStream = File.Open(dataOperationFilePath, FileMode.Create);
                await using var dataOperationWriter = new StreamWriter(dataOperationFileStream);

                foreach (var op in operation.Value) await dataOperationWriter.WriteLineAsync(op);
            }
        }

        _logger.Information("Schedule plans processed successfully");
        return Result<Unit, ProcessorError>.Success(Unit.Value);
    }

    private async Task<Result<(string Output, bool ResetFailed), ProcessorError>> ProcessExecutionPlanAsync(
        SchedulePlan plan)
    {
        _logger.Debug("Processing execution plan...");


        var setScheduleResult = await scheduler.SetScheduleAsync(plan);
        if (setScheduleResult.IsError)
        {
            _logger.Error("Failed to set schedule: {Error}", setScheduleResult.GetErrorOrThrow());
            return Result<(string Output, bool ResetFailed), ProcessorError>.Error(
                new ProcessorError(
                    $"Failed to set schedule for plan {plan.ScheduleId}: {setScheduleResult.GetErrorOrThrow()}")
            );
        }

        var checkResult = await scheduler.CheckIfSerializableAsync();
        if (checkResult.IsError)
        {
            _logger.Error("Failed to check if schedule is serializable: {Error}", checkResult.GetErrorOrThrow());
            return Result<(string Output, bool ResetFailed), ProcessorError>.Error(
                new ProcessorError(
                    $"Failed to check serializability for plan {plan.ScheduleId}: {checkResult.GetErrorOrThrow()}"));
        }

        var outputString = checkResult.GetValueOrThrow();
        _logger.Information("Schedule {ScheduleId} processed: {Output}", plan.ScheduleId, outputString);

        var resetResult = await scheduler.ResetScheduler();
        if (resetResult.IsError)
        {
            _logger.Error("Failed to reset scheduler: {Error}", resetResult.GetErrorOrThrow());
            // Return success with the output but mark reset as failed
            return Result<(string Output, bool ResetFailed), ProcessorError>.Success((outputString, true));
        }

        return Result<(string Output, bool ResetFailed), ProcessorError>.Success((outputString, false));
    }
}
