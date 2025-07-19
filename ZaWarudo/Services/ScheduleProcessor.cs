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

        if (File.Exists(outputPath))
            Log.Warning("the file already exists, it will be overwritten: {OutputPath}", outputPath);

        _logger.Information("Output file created at {OutputPath}", outputPath);

        await using var fileStream = File.Open(outputPath, FileMode.Create);
        await using var writer = new StreamWriter(fileStream);

        foreach (var plan in schedulePlans)
        {
            _logger.Debug("Processing schedule plan with ID {ScheduleId}", plan.ScheduleId);

            var setScheduleResult = await scheduler.SetScheduleAsync(plan);
            if (setScheduleResult.IsError)
            {
                _logger.Error("Failed to set schedule: {Error}", setScheduleResult.GetErrorOrThrow());
                return Result<Unit, ProcessorError>.Error(
                    new ProcessorError(
                        $"Failed to set schedule for plan {plan.ScheduleId}: {setScheduleResult.GetErrorOrThrow()}")
                );
            }

            var checkResult = await scheduler.CheckIfSerializableAsync();
            if (checkResult.IsError)
            {
                _logger.Error("Failed to check if schedule is serializable: {Error}", checkResult.GetErrorOrThrow());
                return Result<Unit, ProcessorError>.Error(
                    new ProcessorError(
                        $"Failed to check serializability for plan {plan.ScheduleId}: {checkResult.GetErrorOrThrow()}"));
            }

            var outputString = checkResult.GetValueOrThrow();
            await writer.WriteLineAsync(outputString);
            _logger.Information("Schedule {ScheduleId} processed: {Output}", plan.ScheduleId, outputString);

            var resetResult = await scheduler.ResetScheduler();
            if (resetResult.IsError)
            {
                _logger.Error("Failed to reset scheduler: {Error}", resetResult.GetErrorOrThrow());
                return Result<Unit, ProcessorError>.Error(
                    new ProcessorError(
                        $"Failed to reset scheduler after processing plan {plan.ScheduleId}: {resetResult.GetErrorOrThrow()}"));
            }
        }

        _logger.Information("Schedule plans processed successfully");
        return Result<Unit, ProcessorError>.Success(Unit.Value);
    }
}
