using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TicketReservationSystem.API.Controllers;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.DTOs;
using TicketReservationSystem.Application.Queries.Events;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Tests;

public class EventsControllerTests
{
    private static readonly DateTime Start = new(2027, 1, 15, 19, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2027, 1, 15, 23, 0, 0, DateTimeKind.Utc);

    private static EventDto CreateEventDto()
    {
        return new EventDto(
            SocialEventId.CreateUnique(),
            "Test Event",
            "Description",
            Start,
            End,
            100,
            80,
            20,
            EventStatus.Scheduled,
            150m,
            "PLN");
    }

    private static EventsController CreateController(Action<Mock<IQueryDispatcher>>? querySetup = null)
    {
        var queryDispatcherMock = new Mock<IQueryDispatcher>();
        querySetup?.Invoke(queryDispatcherMock);

        var controller = new EventsController(queryDispatcherMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetEventById_WhenEventFound_ReturnsOk()
    {
        var eventDto = CreateEventDto();
        var controller = CreateController(querySetup: mock =>
        {
            mock.Setup(d => d.ExecuteAsync<GetEventByIdQuery, GetEventByIdResult>(
                    It.IsAny<GetEventByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetEventByIdResult(eventDto));
        });

        var result = await controller.GetEventById(Guid.NewGuid());

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(eventDto, ok.Value);
    }

    [Fact]
    public async Task GetEventById_WhenEventMissing_ReturnsNotFound()
    {
        var controller = CreateController(querySetup: mock =>
        {
            mock.Setup(d => d.ExecuteAsync<GetEventByIdQuery, GetEventByIdResult>(
                    It.IsAny<GetEventByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetEventByIdResult(null));
        });

        var result = await controller.GetEventById(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetEvents_ReturnsOkWithEvents()
    {
        var events = new List<EventDto> { CreateEventDto(), CreateEventDto() };
        var controller = CreateController(querySetup: mock =>
        {
            mock.Setup(d => d.ExecuteAsync<GetEventsQuery, GetEventsResult>(
                    It.IsAny<GetEventsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetEventsResult(events));
        });

        var result = await controller.GetEvents();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(events, ok.Value);
    }
}
