namespace ZaWarudo.Model;

public readonly struct Operation(OperationType type, string transactionId, string dataId)
{
    public readonly OperationType Type { get; } = type;
    public readonly string TransactionId { get; } = transactionId;
    public readonly string DataId { get; } = dataId;
}

public enum OperationType
{
    Read,
    Write,
    Commit
}
