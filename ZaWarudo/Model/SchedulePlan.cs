namespace ZaWarudo.Model;

public class SchedulePlan(string scheduleId, List<Operation> operations)
{
    public List<Operation> Operations { get; } = operations;
    public string ScheduleId { get; } = scheduleId;

    public override string ToString()
    {
        return $"{ScheduleId}: {string.Join(", ", Operations)}";
    }
}
