using ZaWarudo.Model;

namespace ZaWarudo.Tests.Unit.Models;

public class LogRecordTests
{
    [Fact]
    public void LogRecord_CanBeCreatedWithValidParameters()
    {
        // Arrange
        const string transactionId = "tx123";
        const uint tsRead = 1000u;
        const uint tsWrite = 2000u;
        // Act
        var logRecord = new LogRecord(transactionId, tsRead, tsWrite);

        // Assert
        Assert.NotNull(logRecord);
    }

    [Fact]
    public void IsReadable_ReturnsTrue_WhenTransactionTimestampIsGreaterThanOrEqualToWriteTimestamp()
    {
        // Arrange
        var logRecord = new LogRecord("tx123", 1000u, 2000u);
        var transaction = new TransactionRecord("tx456") { Ts = 2000u };

        // Act
        var result = logRecord.IsReadable(transaction);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsReadable_ReturnsFalse_WhenTransactionTimestampIsLessThanWriteTimestamp()
    {
        // Arrange
        var logRecord = new LogRecord("tx123", 1000u, 2000u);
        var transaction = new TransactionRecord("tx456") { Ts = 1500u };

        // Act
        var result = logRecord.IsReadable(transaction);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsWritable_ReturnsTrue_WhenTransactionTimestampIsGreaterThanOrEqualToReadAndWriteTimestamps()
    {
        // Arrange
        var logRecord = new LogRecord("tx123", 1000u, 2000u);
        var transaction = new TransactionRecord("tx456") { Ts = 2000u };

        // Act
        var result = logRecord.IsWritable(transaction);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsWritable_ReturnsFalse_WhenTransactionTimestampIsLessThanReadOrWriteTimestamps()
    {
        // Arrange
        var logRecord = new LogRecord("tx123", 1000u, 2000u);
        var transaction = new TransactionRecord("tx456") { Ts = 1500u };

        // Act
        var result = logRecord.IsWritable(transaction);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void SetTsRead_UpdatesReadTimestamp_WhenNewTimestampIsGreater()
    {
        // Arrange
        var logRecord = new LogRecord("tx123", 1000u, 2000u);

        // Act
        logRecord.SetTsRead(1500u);

        // Assert
        Assert.Equal(1500u, logRecord.TsRead);
    }

    [Fact]
    public void SetTsRead_DoesNotUpdateReadTimestamp_WhenNewTimestampIsLessThanOrEqual()
    {
        // Arrange
        var logRecord = new LogRecord("tx123", 1000u, 2000u);

        // Act
        logRecord.SetTsRead(1000u);

        // Assert
        Assert.Equal(1000u, logRecord.TsRead);
    }

    [Fact]
    public void ResetRecord_SetsTimestampsToZero()
    {
        // Arrange
        var logRecord = new LogRecord("tx123", 1000u, 2000u);

        // Act
        logRecord.ResetRecord();

        // Assert
        Assert.Equal(0u, logRecord.TsRead);
        Assert.Equal(0u, logRecord.TsWrite);
    }
}
