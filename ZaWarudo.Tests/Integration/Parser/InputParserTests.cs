using JetBrains.Annotations;
using Serilog;
using ZaWarudo.Model;
using ZaWarudo.Parser;

namespace ZaWarudo.Tests.Integration.Parser;

[TestSubject(typeof(InputParser))]
public class InputParserTests
{
    static InputParserTests()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();
    }

    [Fact]
    public void ParseDataRecords_ValidInput_ReturnsSuccessWithCorrectDataRecords()
    {
        // Arrange
        const string line = "A,B,C;";

        // Act
        var result = InputParser.ParseDataRecords(line);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsError);

        var dataRecords = result.GetValueOrThrow();
        Assert.Equal(3, dataRecords.Count);
        Assert.Equal("A", dataRecords[0].Id);
        Assert.Equal("B", dataRecords[1].Id);
        Assert.Equal("C", dataRecords[2].Id);
    }

    [Fact]
    public void ParseDataRecords_InputWithExtraSpacesAndNoSemicolon_ReturnsSuccessWithCorrectDataRecords()
    {
        // Arrange
        const string line = " A , B , C ";

        // Act
        var result = InputParser.ParseDataRecords(line);

        // Assert
        Assert.True(result.IsSuccess);

        var dataRecords = result.GetValueOrThrow();
        Assert.Equal(3, dataRecords.Count);
        Assert.Equal("A", dataRecords[0].Id);
        Assert.Equal("B", dataRecords[1].Id);
        Assert.Equal("C", dataRecords[2].Id);
    }

    [Fact]
    public void ParseDataRecords_EmptyInput_ReturnsError()
    {
        // Arrange
        const string line = "";

        // Act
        var result = InputParser.ParseDataRecords(line);

        // Assert
        Assert.True(result.IsError);

        var error = result.GetErrorOrThrow();
        Assert.Contains("No data records found", error.ToString());
    }

    [Fact]
    public void ParseDataRecords_InputWithOnlySemicolon_ReturnsError()
    {
        // Arrange
        const string line = ";";

        // Act
        var result = InputParser.ParseDataRecords(line);

        // Assert
        Assert.True(result.IsError);

        var error = result.GetErrorOrThrow();
        Assert.Contains("No data records found", error.ToString());
    }

    [Fact]
    public void ParseDataRecords_InputWithOnlySpaces_ReturnsError()
    {
        // Arrange
        const string line = "   ,   ;";

        // Act
        var result = InputParser.ParseDataRecords(line);

        // Assert
        Assert.True(result.IsError);

        var error = result.GetErrorOrThrow();
        Assert.Contains("No data records found", error.ToString());
    }

    // --- Testes para ParseTransactionRecords ---
    [Fact]
    public void ParseTransactionRecords_ValidInput_ReturnsSuccessWithCorrectTransactionRecords()
    {
        // Arrange
        const string transactionLine = "T1,T2,T3";
        const string timestampsLine = "100,200,300";

        // Act
        var result = InputParser.ParseTransactionRecords(transactionLine, timestampsLine);

        // Assert
        Assert.True(result.IsSuccess);

        var transactionRecords = result.GetValueOrThrow();
        Assert.Equal(3, transactionRecords.Count);
        Assert.Equal("T1", transactionRecords[0].Id);
        Assert.Equal(100U, transactionRecords[0].Timestamp);
        Assert.Equal("T2", transactionRecords[1].Id);
        Assert.Equal(200U, transactionRecords[1].Timestamp);
        Assert.Equal("T3", transactionRecords[2].Id);
        Assert.Equal(300U, transactionRecords[2].Timestamp);
    }

    [Fact]
    public void ParseTransactionRecords_EmptyTransactionLine_ReturnsError()
    {
        // Arrange
        const string transactionLine = "";
        const string timestampsLine = "100,200";

        // Act
        var result = InputParser.ParseTransactionRecords(transactionLine, timestampsLine);

        // Assert
        Assert.True(result.IsError);

        var error = result.GetErrorOrThrow();
        Assert.Contains("No transaction records found", error.ToString());
    }

    [Fact]
    public void ParseTransactionRecords_EmptyTimestampsLine_ReturnsError()
    {
        // Arrange
        const string transactionLine = "T1,T2";
        const string timestampsLine = "";

        // Act
        var result = InputParser.ParseTransactionRecords(transactionLine, timestampsLine);

        // Assert
        Assert.True(result.IsError);

        var error = result.GetErrorOrThrow();
        Assert.Contains("No timestamps found", error.ToString());
    }

    [Fact]
    public void ParseTransactionRecords_MismatchedCounts_ReturnsError()
    {
        // Arrange
        const string transactionLine = "T1,T2";
        const string timestampsLine = "100,200,300";

        // Act
        var result = InputParser.ParseTransactionRecords(transactionLine, timestampsLine);

        // Assert
        Assert.True(result.IsError);

        var error = result.GetErrorOrThrow();
        Assert.Contains("Transaction and timestamp counts do not match", error.ToString());
    }

    [Fact]
    public void ParseTransactionRecords_InvalidTimestampFormat_ReturnsError()
    {
        // Arrange
        const string transactionLine = "T1";
        const string timestampsLine = "abc"; // Não é um número

        // Act
        var result = InputParser.ParseTransactionRecords(transactionLine, timestampsLine);

        // Assert
        Assert.True(result.IsError);
        Assert.False(result.IsSuccess);
        var error = result.GetErrorOrThrow();
        Assert.Contains("Failed to parse transaction records", error.ToString());
    }

    [Fact]
    public void ParseSchedulePlan_ValidReadWriteCommitOperations_ReturnsSuccessWithCorrectSchedulePlan()
    {
        // Arrange
        const string scheduleLine = "S1 - r1(A) w2(B) c1";
        const int lineNumber = 5;

        // Act
        var result = InputParser.ParseSchedulePlan(scheduleLine, lineNumber);

        // Assert
        Assert.True(result.IsSuccess);

        var schedulePlan = result.GetValueOrThrow();
        Assert.Equal("S1", schedulePlan.ScheduleId);
        Assert.Equal(3, schedulePlan.Operations.Count);

        Assert.Equal(OperationType.Read, schedulePlan.Operations[0].Type);
        Assert.Equal("T1", schedulePlan.Operations[0].TransactionId);
        Assert.Equal("A", schedulePlan.Operations[0].DataId);

        Assert.Equal(OperationType.Write, schedulePlan.Operations[1].Type);
        Assert.Equal("T2", schedulePlan.Operations[1].TransactionId);
        Assert.Equal("B", schedulePlan.Operations[1].DataId);

        Assert.Equal(OperationType.Commit, schedulePlan.Operations[2].Type);
        Assert.Equal("T1", schedulePlan.Operations[2].TransactionId);
        Assert.Equal(string.Empty, schedulePlan.Operations[2].DataId);
    }

    [Fact]
    public void ParseSchedulePlan_ValidScheduleWithOnlyCommit_ReturnsSuccess()
    {
        // Arrange
        const string scheduleLine = "S2 - c1 c2 c3";
        const int lineNumber = 6;

        // Act
        var result = InputParser.ParseSchedulePlan(scheduleLine, lineNumber);

        // Assert
        Assert.True(result.IsSuccess);

        var schedulePlan = result.GetValueOrThrow();
        Assert.Equal("S2", schedulePlan.ScheduleId);
        Assert.Equal(3, schedulePlan.Operations.Count);

        Assert.Equal(OperationType.Commit, schedulePlan.Operations[0].Type);
        Assert.Equal("T1", schedulePlan.Operations[0].TransactionId);
        Assert.Equal(string.Empty, schedulePlan.Operations[0].DataId);

        Assert.Equal(OperationType.Commit, schedulePlan.Operations[1].Type);
        Assert.Equal("T2", schedulePlan.Operations[1].TransactionId);
        Assert.Equal(string.Empty, schedulePlan.Operations[1].DataId);
    }

    [Fact]
    public void ParseSchedulePlan_InvalidOperationType_ReturnsError()
    {
        // Arrange
        const string scheduleLine = "S3 - x1(A)"; // 'x' é um tipo de operação inválido
        const int lineNumber = 7;

        // Act
        var result = InputParser.ParseSchedulePlan(scheduleLine, lineNumber);

        // Assert
        Assert.True(result.IsError);

        var error = result.GetErrorOrThrow();
        Assert.Contains("Invalid operation type", error.ToString());
        Assert.Contains($"Line {lineNumber}:", error.ToString());
    }

    [Fact]
    public void ParseSchedulePlan_MalformedScheduleLine_ReturnsError()
    {
        // Arrange
        const string scheduleLine = "S4 r1(A)"; // Falta o "-" que é usado no Split
        const int lineNumber = 8;

        // Act
        var result = InputParser.ParseSchedulePlan(scheduleLine, lineNumber);

        // Assert
        Assert.True(result.IsError);

        var error = result.GetErrorOrThrow();
        Assert.Contains("Invalid schedule plan format.", error.ToString());
        Assert.Contains($"Line {lineNumber}:", error.ToString());
    }

    [Fact]
    public void ParseInputReader_ValidFullInput_ReturnsSuccessWithParsedData()
    {
        // Arrange
        const string inputContent = """
                                    A,B,C;
                                    T1,T2,T3;
                                    100,200,300;
                                    S1 - r1(X) w1(Y) c1
                                    S2 - r2(Z) c2
                                    """;
        using var reader = new StringReader(inputContent);

        // Act
        var result = InputParser.ParseInputReader(reader);

        // Assert
        Assert.True(result.IsSuccess);

        // Validate Data Records
        var parsedFile = result.GetValueOrThrow();
        Assert.Equal(3, parsedFile.DataRecords.Count);
        Assert.Equal("A", parsedFile.DataRecords[0].Id);
        Assert.Equal("B", parsedFile.DataRecords[1].Id);

        // Validate Transaction Records
        Assert.Equal(3, parsedFile.TransactionRecords.Count);
        Assert.Equal("T1", parsedFile.TransactionRecords[0].Id);
        Assert.Equal(100U, parsedFile.TransactionRecords[0].Timestamp);
        Assert.Equal("T2", parsedFile.TransactionRecords[1].Id);
        Assert.Equal(200U, parsedFile.TransactionRecords[1].Timestamp);

        // Validate Schedule Plans
        Assert.Equal(2, parsedFile.SchedulePlans.Count);
        Assert.Equal("S1", parsedFile.SchedulePlans[0].ScheduleId);
        Assert.Equal(3, parsedFile.SchedulePlans[0].Operations.Count);
        Assert.Equal(OperationType.Read, parsedFile.SchedulePlans[0].Operations[0].Type);
        Assert.Equal("T1", parsedFile.SchedulePlans[0].Operations[0].TransactionId);
        Assert.Equal("X", parsedFile.SchedulePlans[0].Operations[0].DataId);

        Assert.Equal("S2", parsedFile.SchedulePlans[1].ScheduleId);
        Assert.Equal(2, parsedFile.SchedulePlans[1].Operations.Count);
        Assert.Equal(OperationType.Read, parsedFile.SchedulePlans[1].Operations[0].Type);
        Assert.Equal("T2", parsedFile.SchedulePlans[1].Operations[0].TransactionId);
        Assert.Equal("Z", parsedFile.SchedulePlans[1].Operations[0].DataId);
        Assert.Equal(OperationType.Commit, parsedFile.SchedulePlans[1].Operations[1].Type);
    }

    [Fact]
    public void ParseInputReader_EmptyCsvInput_ReturnsError()
    {
        // Arrange
        const string inputContent = "";
        using var reader = new StringReader(inputContent);

        // Act
        var result = InputParser.ParseInputReader(reader);

        // Assert
        Assert.True(result.IsError);

        Assert.Contains("CSV input is empty", result.GetErrorOrThrow().ToString());
    }

    [Fact]
    public void ParseInputReader_MissingTransactionLine_ReturnsError()
    {
        // Arrange
        const string inputContent = "A,B,C;"; // Missing transaction line
        using var reader = new StringReader(inputContent);

        // Act
        var result = InputParser.ParseInputReader(reader);

        // Assert
        Assert.True(result.IsError);

        Assert.Contains("No transaction records found", result.GetErrorOrThrow().ToString());
    }

    [Fact]
    public void ParseInputReader_MissingTimestampLine_ReturnsError()
    {
        // Arrange
        const string inputContent = """
                                    A,B,C;
                                    T1,T2;
                                    """;
        using var reader = new StringReader(inputContent);

        // Act
        var result = InputParser.ParseInputReader(reader);

        // Assert
        Assert.True(result.IsError);
        Assert.False(result.IsSuccess);
        Assert.Contains("No transaction timestamps found", result.GetErrorOrThrow().ToString());
    }

    [Fact]
    public void ParseInputReader_InvalidSchedulePlanLine_ReturnsError()
    {
        // Arrange
        const string inputContent = """
                                    A,B,C;
                                    T1,T2;
                                    100,200;
                                    S1 - invalid_format
                                    """;
        using var reader = new StringReader(inputContent);

        // Act
        var result = InputParser.ParseInputReader(reader);

        // Assert
        Assert.True(result.IsError);

        Assert.Contains("No operations found in schedule plan", result.GetErrorOrThrow().ToString());

        Assert.Contains("Line 4:", result.GetErrorOrThrow().ToString());
    }

    [Fact]
    public void ParseInputReader_ErrorInDataRecordsParsing_ReturnsError()
    {
        // Arrange
        const string inputContent = """
                                    ;
                                    T1,T2;
                                    100,200;
                                    S1 - r1(X)
                                    """;
        using var reader = new StringReader(inputContent);

        // Act
        var result = InputParser.ParseInputReader(reader);

        // Assert
        Assert.True(result.IsError);

        Assert.Contains("No data records found", result.GetErrorOrThrow().ToString());
    }

    [Fact]
    public void ParseInputReader_ErrorInTransactionRecordsParsing_ReturnsError()
    {
        // Arrange
        const string inputContent = """
                                    A,B,C;
                                    T1,T2;
                                    100,abc;
                                    S1 - r1(X)
                                    """;
        using var reader = new StringReader(inputContent);

        // Act
        var result = InputParser.ParseInputReader(reader);

        // Assert
        Assert.True(result.IsError);

        Assert.Contains("Failed to parse transaction records", result.GetErrorOrThrow().ToString());
    }

    [Fact]
    public void ParseInputReader_HandlesEmptyLinesInSchedulePlans_ReturnsSuccess()
    {
        // Arrange
        const string inputContent = """
                                    A,B,C;
                                    T1,T2;
                                    100,200;

                                    S1 - r1(X)

                                    S2 - w2(Y)
                                    """;
        using var reader = new StringReader(inputContent);

        // Act
        var result = InputParser.ParseInputReader(reader);

        // Assert
        Assert.True(result.IsSuccess);
        var parsedFile = result.GetValueOrThrow();
        Assert.Equal(2, parsedFile.SchedulePlans.Count); // Deve ignorar as linhas vazias
        Assert.Equal("S1", parsedFile.SchedulePlans[0].ScheduleId);
        Assert.Equal("S2", parsedFile.SchedulePlans[1].ScheduleId);
    }

    [Fact]
    public void ParseInput_FileDoesNotExist_ReturnsError()
    {
        // Arrange
        const string nonExistentFilePath = "nonexistent_file.txt";

        // Act
        var result = InputParser.ParseInput(nonExistentFilePath);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains($"Input file not found: {nonExistentFilePath}", result.GetErrorOrThrow().ToString());
    }

    [Fact]
    public void ParseInput_ValidFile_ReturnsSuccessWithParsedData()
    {
        // Arrange
        const string filePath = "test_valid_input.txt";
        const string fileContent = """
                                   A,B,C;
                                   T1,T2,T3;
                                   100,200,300;
                                   S1 - r1(X) w1(Y) c1
                                   S2 - r2(Z) c2
                                   """;
        File.WriteAllText(filePath, fileContent);

        try
        {
            // Act
            var result = InputParser.ParseInput(filePath);

            // Assert
            Assert.True(result.IsSuccess);
            var parsedFile = result.GetValueOrThrow();

            Assert.Equal(3, parsedFile.DataRecords.Count);
            Assert.Equal(3, parsedFile.TransactionRecords.Count);
            Assert.Equal(2, parsedFile.SchedulePlans.Count);
        }
        finally
        {
            // Clean up: Remove the test file
            File.Delete(filePath);
        }
    }

    [Fact]
    public void ParseInput_FileWithInvalidContent_ReturnsError()
    {
        // Arrange
        const string filePath = "test_invalid_content.txt";
        const string fileContent = """
                                   ;
                                   T1,T2;
                                   100,200;
                                   """;
        File.WriteAllText(filePath, fileContent);

        try
        {
            // Act
            var result = InputParser.ParseInput(filePath);

            // Assert
            Assert.True(result.IsError);

            Assert.Contains("No data records found", result.GetErrorOrThrow().ToString());
        }
        finally
        {
            // Clean up: Remove the test file
            File.Delete(filePath);
        }
    }

    [Fact]
    public void ParseInput_FileCannotBeRead_ReturnsError()
    {
        // Arrange
        const string filePath = "test_unreadable_file.txt";

        File.WriteAllText(filePath, "some content");
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None);

            // Act
            var result = InputParser.ParseInput(filePath);

            // Assert
            Assert.True(result.IsError);
            Assert.Contains("Failed to read input file", result.GetErrorOrThrow().ToString());
        }
        catch (Exception ex)
        {
            Assert.Fail($"Could not set up exclusive file lock: {ex.Message}");
        }
        finally
        {
            File.Delete(filePath);
        }
    }
}
