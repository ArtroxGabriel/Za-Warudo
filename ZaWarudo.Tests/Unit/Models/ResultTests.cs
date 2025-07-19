using ZaWarudo.Model;

namespace ZaWarudo.Tests.Unit.Models;

public class ResultTests
{
    [Fact]
    public void Success_SetsIsSuccessToTrue()
    {
        // Arrange & Act
        var result = Result<int, string>.Success(42);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsError);
        Assert.Equal(42, result.GetValueOrThrow());
    }

    [Fact]
    public void Error_SetsIsErrorToTrue()
    {
        // Arrange & Act
        var result = Result<int, string>.Error("An error occurred");

        // Assert
        Assert.True(result.IsError);
        Assert.False(result.IsSuccess);
        Assert.Equal("An error occurred", result.GetErrorOrThrow());
    }

    [Fact]
    public void GetValueOrThrow_OnErrorResult_ThrowsInvalidOperationException()
    {
        // Arrange
        var result = Result<int, string>.Error("An error occurred");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => result.GetValueOrThrow());

        Assert.Contains("Cannot get value from error result", exception.Message);
    }

    [Fact]
    public void GetErrorOrThrow_OnSuccessResult_ThrowsInvalidOperationException()
    {
        // Arrange
        var result = Result<int, string>.Success(42);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => result.GetErrorOrThrow());
        Assert.Equal("Cannot get error from success result", exception.Message);
    }

    [Fact]
    public void Match_OnSuccessResult_CallsSuccessAction()
    {
        // Arrange
        var result = Result<int, string>.Success(42);
        var successCalled = false;
        var errorCalled = false;

        // Act
        result.Match(
            value =>
            {
                successCalled = true;
                Assert.Equal(42, value);
            },
            _ => { errorCalled = true; }
        );

        // Assert
        Assert.True(successCalled);
        Assert.False(errorCalled);
    }

    [Fact]
    public void Match_OnErrorResult_CallsErrorAction()
    {
        // Arrange
        var result = Result<int, string>.Error("An error occurred");
        var successCalled = false;
        var errorCalled = false;

        // Act
        result.Match(
            _ => { successCalled = true; },
            error =>
            {
                errorCalled = true;
                Assert.Equal("An error occurred", error);
            }
        );

        // Assert
        Assert.False(successCalled);
        Assert.True(errorCalled);
    }

    [Fact]
    public void Match_WithResultFunction_OnSuccessResult_ReturnsSuccessFunctionResult()
    {
        // Arrange
        var result = Result<int, string>.Success(42);

        // Act
        var matchResult = result.Match(
            value => value * 2,
            _ => -1
        );

        // Assert
        Assert.Equal(84, matchResult);
    }

    [Fact]
    public void Match_WithResultFunction_OnErrorResult_ReturnsErrorFunctionResult()
    {
        // Arrange
        var result = Result<int, string>.Error("An error occurred");

        // Act
        var matchResult = result.Match(
            value => value * 2,
            _ => -1
        );

        // Assert
        Assert.Equal(-1, matchResult);
    }

    [Fact]
    public void Map_OnSuccessResult_TransformsValue()
    {
        // Arrange
        var result = Result<int, string>.Success(42);

        // Act
        var mapped = result.Map(value => value.ToString());

        // Assert
        Assert.True(mapped.IsSuccess);
        Assert.Equal("42", mapped.GetValueOrThrow());
    }

    [Fact]
    public void Map_OnErrorResult_PreservesError()
    {
        // Arrange
        var result = Result<int, string>.Error("An error occurred");

        // Act
        var mapped = result.Map(value => value.ToString());

        // Assert
        Assert.True(mapped.IsError);
        Assert.Equal("An error occurred", mapped.GetErrorOrThrow());
    }

    [Fact]
    public void Bind_OnSuccessResult_TransformsToNewResult()
    {
        // Arrange
        var result = Result<int, string>.Success(42);

        // Act
        var bound = result.Bind(value => Result<string, string>.Success(value.ToString()));

        // Assert
        Assert.True(bound.IsSuccess);
        Assert.Equal("42", bound.GetValueOrThrow());
    }

    [Fact]
    public void Bind_OnSuccessResult_TransformsToErrorResult()
    {
        // Arrange
        var result = Result<int, string>.Success(42);

        // Act
        var bound = result.Bind(_ => Result<string, string>.Error("New error"));

        // Assert
        Assert.True(bound.IsError);
        Assert.Equal("New error", bound.GetErrorOrThrow());
    }

    [Fact]
    public void Bind_OnErrorResult_PreservesError()
    {
        // Arrange
        var result = Result<int, string>.Error("Original error");

        // Act
        var bound = result.Bind(value => Result<string, string>.Success(value.ToString()));

        // Assert
        Assert.True(bound.IsError);
        Assert.Equal("Original error", bound.GetErrorOrThrow());
    }
}
