using ZaWarudo.Model;

namespace ZaWarudo.Services;

public interface ISchedulerProcessor
{
    public Task<Result<Unit, ProcessorError>> ProcessScheduleAsync(IEnumerable<SchedulePlan> schedulePlans,
        string outputPath);
}

public readonly struct ProcessorError(string message)
{
    private string Message { get; } = message;

    public override string ToString()
    {
        return Message;
    }
}
