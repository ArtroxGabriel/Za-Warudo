namespace ZaWarudo.Model;

public class LogRecord(string idData, uint tsRead, uint tsWrite)
{
    public LogRecord() : this(string.Empty, 0, 0)
    {
    }

    private string IdData { get; init; } = idData;
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
        if (TsRead < ts) TsRead = ts;
    }

    public void SetTsWrite(uint ts)
    {
        TsWrite = ts;
    }


    public virtual void ResetRecord()
    {
        TsRead = 0;
        TsWrite = 0;
    }
}
