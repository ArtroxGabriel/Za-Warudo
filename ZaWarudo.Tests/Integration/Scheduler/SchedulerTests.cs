using Serilog;
using ZaWarudo.Model;

namespace ZaWarudo.Tests.Integration.Scheduler;

public class SchedulerTests
{
    public SchedulerTests()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();
    }

    [Fact]
    public async Task SetScheduleAsync_WithValidInput_ShouldSucceed()
    {
        // Arrange
        const string scheduleId = "S1";
        var operations = new List<Operation>
        {
            new(
                OperationType.Read,
                "T1",
                "X"
            ),
            new(
                OperationType.Read,
                "T2",
                "Y"
            )
        };


        var _scheduler = new ZaWarudo.Scheduler.Scheduler();

        // Act
        var result = await _scheduler.SetScheduleAsync(scheduleId, operations);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ResetScheduler_WhenCalled_ShouldAllowNewScheduleToBeSet()
    {
        // Arrange
        var _scheduler = new ZaWarudo.Scheduler.Scheduler();
        var initialOperations = new List<Operation> { new(OperationType.Read, "T1", "X") };

        await _scheduler.SetScheduleAsync("S1", initialOperations);

        // Act
        var resetResult = await _scheduler.ResetScheduler();

        // Assert
        Assert.True(resetResult.IsSuccess);

        // To verify the reset, we can now set a new schedule.
        // If the scheduler was not reset, this might fail depending on its internal logic.
        var newOperations = new List<Operation> { new(OperationType.Write, "T2", "Y") };
        var newScheduleResult = await _scheduler.SetScheduleAsync("S2", newOperations);
        Assert.True(newScheduleResult.IsSuccess);
    }
}
