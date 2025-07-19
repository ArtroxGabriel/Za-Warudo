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

        var schedulePlan = new SchedulePlan(scheduleId, operations);


        var _scheduler = new ZaWarudo.Scheduler.Scheduler();

        // Act
        var result = await _scheduler.SetScheduleAsync(schedulePlan);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ResetScheduler_WhenCalled_ShouldAllowNewScheduleToBeSet()
    {
        // Arrange
        var _scheduler = new ZaWarudo.Scheduler.Scheduler();
        var initialOperations = new List<Operation> { new(OperationType.Read, "T1", "X") };
        var initialSchedulePlan = new SchedulePlan("S1", initialOperations);

        await _scheduler.SetScheduleAsync(initialSchedulePlan);

        // Act
        var resetResult = await _scheduler.ResetScheduler();

        // Assert
        Assert.True(resetResult.IsSuccess);

        var newOperations = new List<Operation> { new(OperationType.Write, "T2", "Y") };
        var newSchedulePlan = new SchedulePlan("S2", newOperations);
        var newScheduleResult = await _scheduler.SetScheduleAsync(newSchedulePlan);
        Assert.True(newScheduleResult.IsSuccess);
    }
}
