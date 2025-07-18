namespace ZaWarudo.Model;

public class TransactionRecord(string id)
{
    public string Id { get; } = id;
    public uint Ts { get; set; } = 0;
}
