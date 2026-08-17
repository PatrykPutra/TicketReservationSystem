using MediatR;
using Moq;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.DTOs;
using TicketReservationSystem.Application.Queries;
using TicketReservationSystem.Application.Queries.Events;

namespace TicketReservationSystem.Tests;

public class QueryDispatcherTests
{
    [Fact]
    public async Task ExecuteAsync_ForQuery_ForwardsToMediatorAndReturnsResult()
    {
        var mediator = new Mock<IMediator>();
        var query = new GetEventsQuery();
        var expected = new GetEventsResult(new List<EventDto>());
        mediator.Setup(m => m.Send(query, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var dispatcher = new QueryDispatcher(mediator.Object);

        var result = await dispatcher.ExecuteAsync<GetEventsQuery, GetEventsResult>(query, CancellationToken.None);

        Assert.Same(expected, result);
        mediator.Verify(m => m.Send(query, It.IsAny<CancellationToken>()), Times.Once);
    }
}