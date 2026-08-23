using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;
using TicketReservationSystem.API.Middleware;

namespace TicketReservationSystem.Tests;

public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenNextSucceeds_PassesThrough()
    {
        // Arrange
        var httpContext = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() }
        };

        var nextCalled = false;
        RequestDelegate nextRequestDelegate = context =>
        {
            nextCalled = true;
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        };
        var exceptionHandlingMiddleware = new ExceptionHandlingMiddleware(
            nextRequestDelegate, NullLogger<ExceptionHandlingMiddleware>.Instance);

        // Act
        await exceptionHandlingMiddleware.InvokeAsync(httpContext);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status204NoContent, httpContext.Response.StatusCode);
        Assert.Equal(0, httpContext.Response.Body.Length);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_Sets500Response()
    {
        // Arrange
        var httpContext = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() }
        };

        RequestDelegate nextRequestDelegate = (context) => throw new InvalidOperationException("boom");
        
        var exceptionHandlingMiddleware = new ExceptionHandlingMiddleware(
            nextRequestDelegate, NullLogger<ExceptionHandlingMiddleware>.Instance);

        // Act
        await exceptionHandlingMiddleware.InvokeAsync(httpContext);
        
        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
        Assert.Equal("application/problem+json", httpContext.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_WritesProblemDetailsBody()
    {
        // Arrange
        var httpContext = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() }
        };

        RequestDelegate nextRequestDelegate = (context) => throw new InvalidOperationException("boom");

        var exceptionHandlingMiddleware = new ExceptionHandlingMiddleware(
            nextRequestDelegate, NullLogger<ExceptionHandlingMiddleware>.Instance);

        // Act
        await exceptionHandlingMiddleware.InvokeAsync(httpContext);
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        // Assert
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("An unexpected error occurred", doc.RootElement.GetProperty("title").GetString());
        Assert.Equal(StatusCodes.Status500InternalServerError, doc.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_LogsError()
    {
        // Arrange
        var httpContext = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() }
        };

        RequestDelegate nextRequestDelegate = (context) => throw new InvalidOperationException("boom");
        var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();

        var exceptionHandlingMiddleware = new ExceptionHandlingMiddleware(
            nextRequestDelegate, logger.Object);

        // Act
        await exceptionHandlingMiddleware.InvokeAsync(httpContext);
        
        //Assert
        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.Is<InvalidOperationException>(e => e.Message == "boom"),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
