namespace ZaWarudo.Model;

public class DataRecord(string id, uint tsRead, uint tsWrite)
{
    public DataRecord()
        : this(string.Empty, 0, 0)
    {
    }

    public DataRecord(string id)
        : this(id, 0, 0)
    {
    }

    public string Id { get; init; } = id;
    public uint TsRead { get; set; } = tsRead;
    public uint TsWrite { get; set; } = tsWrite;

    public bool IsReadable(TransactionRecord tx)
    {
        return !(tx.Ts < TsWrite);
    }

    public bool IsWritable(TransactionRecord tx)
    {
        return !(tx.Ts < TsRead || tx.Ts < TsWrite);
    }

    public void SetTsRead(uint ts)
    {
        if (TsRead < ts)
            TsRead = ts;
    }

    public void SetTsWrite(uint ts)
    {
        TsWrite = ts;
    }

    public void ResetRecord()
    {
        TsRead = 0;
        TsWrite = 0;
    }

    public override string ToString()
    {
        // <ID-dado, TS-Read, TS-Write>
        return $"<{Id}, {TsRead}, {TsWrite}>";
    }
}
