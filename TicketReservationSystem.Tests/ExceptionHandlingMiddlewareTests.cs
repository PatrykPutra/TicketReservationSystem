using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TicketReservationSystem.API.Middleware;

namespace TicketReservationSystem.Tests;

public class ExceptionHandlingMiddlewareTests
{
    private static ExceptionHandlingMiddleware CreateMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware>? logger = null)
    {
        return new ExceptionHandlingMiddleware(
            next,
            logger ?? NullLogger<ExceptionHandlingMiddleware>.Instance);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextSucceeds_PassesThrough()
    {
        var context = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() }
        };
        var nextCalled = false;
        var middleware = CreateMiddleware(ctx =>
        {
            nextCalled = true;
            ctx.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_Sets500Response()
    {
        var context = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() }
        };
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("boom"));

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_WritesProblemDetailsBody()
    {
        var context = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() }
        };
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("boom"));

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        using var doc = JsonDocument.Parse(body);
        Assert.Equal("An unexpected error occurred", doc.RootElement.GetProperty("title").GetString());
        Assert.Equal(StatusCodes.Status500InternalServerError, doc.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_LogsError()
    {
        var context = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() }
        };
        var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("boom"), logger.Object);

        await middleware.InvokeAsync(context);

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
