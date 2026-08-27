using MediatR;
using Moq;
using TicketReservationSystem.Application.DTOs;
using TicketReservationSystem.Application.Queries;
using TicketReservationSystem.Application.Queries.Events;

namespace TicketReservationSystem.Tests;

public class QueryDispatcherTests
{
    [Fact]
    public async Task ExecuteAsync_WhenInvoked_CallsMediatorSend()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        var query = new GetEventsQuery();
        var expected = new GetEventsResult(new List<EventDto>());
        mediator.Setup(m => m.Send(query, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var dispatcher = new QueryDispatcher(mediator.Object);

        // Act
        var result = await dispatcher.ExecuteAsync<GetEventsQuery, GetEventsResult>(query, CancellationToken.None);

        // Assert
        mediator.Verify(m => m.Send(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenInvoked_ReturnsResult()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        var query = new GetEventsQuery();
        var expected = new GetEventsResult(new List<EventDto>());
        mediator.Setup(m => m.Send(query, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var dispatcher = new QueryDispatcher(mediator.Object);

        // Act
        var result = await dispatcher.ExecuteAsync<GetEventsQuery, GetEventsResult>(query, CancellationToken.None);

        // Assert
        Assert.Same(expected, result);
    }
}