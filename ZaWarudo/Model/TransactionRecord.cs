namespace ZaWarudo.Model;

public class TransactionRecord(string id, uint ts = 0)
{
    public string Id { get; } = id;
    public uint Ts { get; set; } =ts;
}
