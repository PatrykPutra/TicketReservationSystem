using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Errors;

namespace TicketReservationSystem.Tests;

public class ResultTests
{
    private sealed class TestResult(bool isSuccess, Error error) : Result(isSuccess, error)
    {
    }

    [Fact]
    public void Result_ForSuccessWithNonNoneError_Throws()
    {
        // Arrange && Act && Assert
        Assert.Throws<ArgumentException>(() => new TestResult(true, new NotFoundError("x")));
    }

    [Fact]
    public void Result_ForFailureWithNoneError_Throws()
    {
        // Arrange && Act && Assert
        Assert.Throws<ArgumentException>(() => new TestResult(false, new NoneError()));
    }

    [Fact]
    public void Result_ForSuccessWithNoneError_Succeeds()
    {
        // Arrange && Act
        var result = new TestResult(true, new NoneError());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.IsType<NoneError>(result.Error);
    }
}