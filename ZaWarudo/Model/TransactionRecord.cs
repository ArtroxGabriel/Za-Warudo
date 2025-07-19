namespace ZaWarudo.Model;

public class TransactionRecord(string id, uint timestamp = 0)
{
    public string Id { get; } = id;
    public uint Timestamp { get; set; } = timestamp;
}
