namespace ZaWarudo.Model;

public readonly struct Operation(OperationType operationType, string transactionId, string dataId)
{
    public readonly OperationType OperationType { get; } = operationType;
    public readonly string TransactionId { get; } = transactionId;
    public readonly string DataId { get; } = dataId;
}

public enum OperationType
{
    Read,
    Write,
    Commit
}
